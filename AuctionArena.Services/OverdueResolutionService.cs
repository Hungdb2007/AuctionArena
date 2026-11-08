using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
namespace AuctionArena.Services
{
    // Chỉ đọc dữ liệu để thu ngân xác nhận trước khi xử lý tiền cọc.
    public sealed class OverdueReauctionPreview
    {
        public int InvoiceId
        {
            get;
            set;
        }
        public string InvoiceCode
        {
            get;
            set;
        }
        public int LotCount
        {
            get;
            set;
        }
        public decimal HammerTotal
        {
            get;
            set;
        }
        public decimal AppliedDeposit
        {
            get;
            set;
        }
        public decimal AdditionalForfeiture
        {
            get;
            set;
        }
        public decimal AlreadyForfeited
        {
            get;
            set;
        }
        public decimal TotalForfeiture
        {
            get
            {
                return AppliedDeposit+AdditionalForfeiture;
            }
        }
    }
    // Giữ hóa đơn gốc khi xử lý vi phạm và chuyển lô sang phiên khác.
    // không chuyển quyền mua tự động vì UQ_InvoicedLot chặn ghi hóa đơn thứ hai lên cùng lô.
    public sealed class OverdueResolutionService : ServiceBase
    {
        public OverdueResolutionService(Func<AuctionArenaDbContext> factory = null):base(factory)
        {
        }
        private sealed class ReauctionCase
        {
            public InvoiceEntity Invoice;
            public RegistrationEntity Registration;
            public List<AuctionLotEntity> Lots;
            public List<ItemEntity> Items;
            public OverdueReauctionPreview Preview;
        }
        // Tất cả điều kiện được áp dụng giống nhau cho xem trước và xử lý thực tế.
        // Xử lý thực tế bắt buộc kiểm tra lại bên trong transaction SERIALIZABLE.
        private static ReauctionCase Validate(AuctionArenaDbContext db, int invoiceId) //Kiểm tra điều kiện xử lý nghiệp vụ trước khi ghi(73-116)
        {
            var invoice = db.Invoices.SingleOrDefault(x => x.InvoiceId == invoiceId);
            if (invoice == null || invoice.InvoiceStatus != "Overdue" || invoice.DueAt>=Now() || 
            invoice.PaidAmount != 0m || invoice.TotalDue<=0m)
            throw new BusinessException("Chỉ xử lý hóa đơn quá hạn đã ghi nhận vi phạm và chưa thu thêm tiền.");
            if (db.StockIssues.Any(x => x.InvoiceId == invoiceId) || 
            db.InvoiceAdjustments.Any(x => x.InvoiceId == invoiceId) || 
            db.Payments.Any(x => x.InvoiceId == invoiceId && x.PaymentType == "Invoice"))
            throw new BusinessException("Đã có giao hàng, thanh toán thực tế hoặc chứng từ điều chỉnh: phải đối soát thủ công.");
            var details = db.InvoiceDetails.Where(x => x.InvoiceId == invoiceId).ToList();
            if (details.Count == 0)
            {
                throw new BusinessException("Hóa đơn không có lot.");
            }
            var lotIds = details.Select(x => x.LotId).ToList();
            var lots = db.AuctionLots.Where(x => lotIds.Contains(x.LotId)).ToList();
            if (lots.Count != details.Count || 
            lots.Any(x => x.LotStatus != "Sold" || x.WinningCustomerId != invoice.CustomerId) || 
            db.SellerPayouts.Any(x => lotIds.Contains(x.LotId)))
            throw new BusinessException("Lot đã thay đổi người thắng hoặc phát sinh chi trả chủ hàng.");
            var itemIds = lots.Select(x => x.ItemId).ToList();
            var items = db.Items.Where(x => itemIds.Contains(x.ItemId)).ToList();
            if (items.Count != lots.Count || items.Any(x => x.ItemStatus != "Sold") || 
            db.ConsignorReturns.Any(x => itemIds.Contains(x.ItemId)) || 
            db.StockIssueDetails.Any(x => itemIds.Contains(x.ItemId)))
            throw new BusinessException("Hiện vật không còn đủ điều kiện đưa về kho.");
            var reg = db.Registrations.SingleOrDefault(x => x.SessionId == invoice.SessionId && x.CustomerId == invoice.CustomerId);
            if (reg == null || reg.DepositApplied<invoice.DepositApplied)
            throw new BusinessException("Số dư chuyển cọc không khớp hóa đơn; cần đối soát thủ công.");
            decimal available = reg.DepositPaid-reg.DepositApplied-reg.DepositForfeited-reg.DepositRefunded;
            if (available<0m || invoice.DepositApplied<0m || invoice.DepositApplied>reg.DepositApplied)
            throw new BusinessException("Số dư cọc không hợp lệ, không thể xử lý tự động.");
            var preview = new OverdueReauctionPreview
            {
                InvoiceId = invoiceId, InvoiceCode = invoice.InvoiceCode, LotCount = lots.Count, 
                HammerTotal = details.Sum(x => x.HammerPrice), AppliedDeposit = invoice.DepositApplied, 
                AdditionalForfeiture = available, AlreadyForfeited = reg.DepositForfeited
            };
            return new ReauctionCase
            {
                Invoice = invoice, Registration = reg, Lots = lots, Items = items, Preview = preview
            };
        }
        public OverdueReauctionPreview Preview(int actor, int invoiceId) //Tính thông tin xem trước, chưa ghi chứng từ
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "Edit");
                SecurityService.Require(db, actor, "SESSIONS", "Edit");
                SecurityService.Require(db, actor, "REGISTRATIONS", "Edit");
                return Validate(db, invoiceId).Preview;
            }
        }
        public int CancelUnpaidOverdueForReauction(int actor, int invoiceId) //Hủy hóa đơn quá hạn chưa thu và chuẩn bị tái đấu giá(127-204)
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Edit");
                    SecurityService.Require(db, actor, "SESSIONS", "Edit");
                    SecurityService.Require(db, actor, "REGISTRATIONS", "Edit");
                    // Khi xác nhận, phải đọc lại điều kiện trong transaction vì dữ liệu có thể đã thay đổi.
                    var current = Validate(db, invoiceId);
                    var invoice = current.Invoice;
                    var reg = current.Registration;
                    var applied = current.Preview.AppliedDeposit;
                    if (applied>0m)
                    {
                        reg.DepositApplied-=applied;
                        reg.DepositForfeited+=applied;
                        db.InvoiceAdjustments.Add(new InvoiceAdjustmentEntity
                        {
                            AdjustmentCode = Code("DC"), InvoiceId = invoiceId, RegistrationId = reg.RegistrationId, 
                            AdjustmentType = "DepositTransferForfeit", Amount = applied, 
                            Reason = "Đảo chuyển cọc và tịch thu theo biên bản quá hạn", CreatedAt = Now(), CreatedByUserId = actor
                        });
                        db.Payments.Add(new PaymentEntity
                        {
                            PaymentCode = Code("PT"), InvoiceId = invoiceId, 
                            RegistrationId = reg.RegistrationId, PaymentType = "Forfeit", PaymentMethod = "Internal", 
                            Amount = applied, PaidAt = Now(), ReceivedByUserId = actor, 
                            Notes = "Tịch thu cọc đã chuyển vào hóa đơn, có InvoiceAdjustments đối ứng"
                        });
                    }
                    var available = current.Preview.AdditionalForfeiture;
                    if (available>0m)
                    {
                        reg.DepositForfeited+=available;
                        db.Payments.Add(new PaymentEntity
                        {
                            PaymentCode = Code("PT"), RegistrationId = reg.RegistrationId, 
                            PaymentType = "Forfeit", PaymentMethod = "Internal", Amount = available, 
                            PaidAt = Now(), ReceivedByUserId = actor, Notes = "Tịch thu số dư cọc còn lại do quá hạn"
                        });
                    }
                    if (reg.DepositPaid != reg.DepositApplied+reg.DepositRefunded+reg.DepositForfeited)
                    throw new BusinessException("Tiền cọc sau đối soát không khớp, hủy toàn bộ thao tác.");
                    reg.RegistrationStatus = "Blocked";
                    invoice.InvoiceStatus = "Cancelled";
                    // Giữ hóa đơn/chi tiết và các chứng từ để truy vết.
                    foreach (var lot in current.Lots)lot.LotStatus = "Defaulted";
                    foreach (var item in current.Items)item.ItemStatus = "InStock";
                    db.Customers.Single(x => x.CustomerId == invoice.CustomerId).HasOutstandingDebt = true;
                    if (!db.CustomerViolations.Any(x => x.CustomerId == invoice.CustomerId && 
                    x.SessionId == invoice.SessionId && x.ViolationType == "NonPayment"))
                    db.CustomerViolations.Add(new CustomerViolationEntity
                    {
                        CustomerId = invoice.CustomerId, 
                        SessionId = invoice.SessionId, ViolationType = "NonPayment", 
                        Description = "Bỏ cọc, hủy hóa đơn "+invoice.InvoiceCode, OccurredAt = Now()
                    });
                    Audit(db, actor, "DEFAULT_REAUCTION", "Invoices", invoiceId, 
                    "Hủy hóa đơn quá hạn "+invoice.InvoiceCode+", tịch thu cọc "+
                    current.Preview.TotalForfeiture.ToString("N0")+", đưa "+current.Lots.Count+" hiện vật về kho");
                    db.SaveChanges();
                    tx.Commit();
                    return current.Lots.Count;
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Dữ liệu đã thay đổi trên máy khác, hãy tải lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
}
