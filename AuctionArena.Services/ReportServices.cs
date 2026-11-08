using System;
using System.Collections.Generic;
using System.Linq;
using AuctionArena.Data;
namespace AuctionArena.Services
{
    public sealed class BidTranscriptRow
    {
        public string SessionName
        {
            get;
            set;
        }
        public int LotNumber
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public DateTime BidAt
        {
            get;
            set;
        }
        public string CustomerName
        {
            get;
            set;
        }
        public int PaddleNumber
        {
            get;
            set;
        }
        public decimal Amount
        {
            get;
            set;
        }
    }
    public sealed class PaymentReceiptRow
    {
        public string PaymentCode
        {
            get;
            set;
        }
        public string InvoiceCode
        {
            get;
            set;
        }
        public string CustomerName
        {
            get;
            set;
        }
        public DateTime PaidAt
        {
            get;
            set;
        }
        public decimal Amount
        {
            get;
            set;
        }
        public string PaymentMethod
        {
            get;
            set;
        }
        public string TransactionReference
        {
            get;
            set;
        }
    }
    public sealed class SellerPayoutReceiptRow
    {
        public string PayoutCode
        {
            get;
            set;
        }
        public string ConsignorName
        {
            get;
            set;
        }
        public int LotNumber
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public DateTime PaidAt
        {
            get;
            set;
        }
        public decimal HammerPrice
        {
            get;
            set;
        }
        public decimal SellerFee
        {
            get;
            set;
        }
        public decimal PayoutAmount
        {
            get;
            set;
        }
        public string PaymentMethod
        {
            get;
            set;
        }
        public string TransferReference
        {
            get;
            set;
        }
    }
    public sealed class InvoiceReportRow
    {
        public string InvoiceCode
        {
            get;
            set;
        }
        public string CustomerName
        {
            get;
            set;
        }
        public string SessionName
        {
            get;
            set;
        }
        public DateTime IssuedAt
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public decimal HammerPrice
        {
            get;
            set;
        }
        public decimal HammerSubtotal
        {
            get;
            set;
        }
        public decimal BuyerFee
        {
            get;
            set;
        }
        public decimal VipDiscount
        {
            get;
            set;
        }
        public decimal VatAmount
        {
            get;
            set;
        }
        public decimal DepositApplied
        {
            get;
            set;
        }
        public decimal TotalDue
        {
            get;
            set;
        }
        public decimal PaidAmount
        {
            get;
            set;
        }
    }
    public sealed class IssueReportRow
    {
        public string IssueCode
        {
            get;
            set;
        }
        public string InvoiceCode
        {
            get;
            set;
        }
        public string RecipientName
        {
            get;
            set;
        }
        public string RecipientDocument
        {
            get;
            set;
        }
        public string RecipientRelation
        {
            get;
            set;
        }
        public string DeliveryMethod
        {
            get;
            set;
        }
        public string ConditionAtDelivery
        {
            get;
            set;
        }
        public string LocationAtIssue
        {
            get;
            set;
        }
        public DateTime IssuedAt
        {
            get;
            set;
        }
        public string ItemCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
    }
    public sealed class RevenueReportRow
    {
        public string SessionName
        {
            get;
            set;
        }
        public DateTime IssuedAt
        {
            get;
            set;
        }
        public string CategoryName
        {
            get;
            set;
        }
        public decimal HammerTotal
        {
            get;
            set;
        }
        public decimal BuyerFee
        {
            get;
            set;
        }
        public decimal SellerFee
        {
            get;
            set;
        }
        public decimal PlatformRevenue
        {
            get;
            set;
        }
        // Số hóa đơn duy nhất trong mỗi nhóm ngày/phiên/loại hiện vật.
        public int InvoiceCount
        {
            get;
            set;
        }
    }
    public sealed class ConsignorReturnReportRow
    {
        public string ReturnCode
        {
            get;
            set;
        }
        public string ItemCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public string ConsignorName
        {
            get;
            set;
        }
        public string RecipientName
        {
            get;
            set;
        }
        public DateTime ReturnedAt
        {
            get;
            set;
        }
        public string Notes
        {
            get;
            set;
        }
    }
    public sealed class ContractReportRow
    {
        public string ContractCode
        {
            get;
            set;
        }
        public string OwnerName
        {
            get;
            set;
        }
        public DateTime SignedAt
        {
            get;
            set;
        }
        public string ItemCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
        public decimal? SellerFeeRate
        {
            get;
            set;
        }
        public DateTime? ExpiresAt
        {
            get;
            set;
        }
    }
    public sealed class ReportService
    {
        // Trả quyền in để khóa Print/Export ngay trong ReportViewer.
        public bool CanPrint(int actor, string permission) //Kiểm tra quyền in phân hệ.
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, permission, "View");
                int roleId = db.Users.Where(u => u.UserId == actor && u.IsActive).Select(u => u.RoleId).Single();
                return (from rp in db.RolePermissions
                join p in db.Permissions on rp.PermissionId equals p.PermissionId
                where rp.RoleId == roleId && p.PermissionCode == permission
                select rp.CanPrint).FirstOrDefault();
            }
        }
        public List<BidTranscriptRow> BidTranscript(int actor, int lotId) //Tạo dữ liệu biên bản trả giá.
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "BIDS", "View");
                return (from b in db.Bids
                join l in db.AuctionLots on b.LotId equals l.LotId
                join sess in db.AuctionSessions on l.SessionId equals sess.SessionId
                join item in db.Items on l.ItemId equals item.ItemId
                join cust in db.Customers on b.CustomerId equals cust.CustomerId
                join reg in db.Registrations on b.RegistrationId equals reg.RegistrationId
                where b.LotId == lotId
                orderby b.BidAt, b.BidId
                select new BidTranscriptRow
                {
                    SessionName = sess.SessionName, LotNumber = l.LotNumber, 
                    ItemName = item.ItemName, BidAt = b.BidAt, CustomerName = cust.FullName, 
                    PaddleNumber = reg.PaddleNumber, Amount = b.Amount
                }).ToList();
            }
        }
        public List<PaymentReceiptRow> PaymentReceipt(int actor, int paymentId) //Tạo dữ liệu phiếu thu.
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                var result = (from p in db.Payments
                join inv in db.Invoices on p.InvoiceId equals inv.InvoiceId
                join c in db.Customers on inv.CustomerId equals c.CustomerId
                where p.PaymentId == paymentId && p.PaymentType == "Invoice"
                select new PaymentReceiptRow
                {
                    PaymentCode = p.PaymentCode, 
                    InvoiceCode = inv.InvoiceCode, CustomerName = c.FullName, PaidAt = p.PaidAt, 
                    Amount = p.Amount, PaymentMethod = p.PaymentMethod, 
                    TransactionReference = p.TransactionReference
                }).ToList();
                if (result.Count != 1)
                {
                    throw new BusinessException("Không tìm thấy phiếu thu thanh toán đã ghi nhận.");
                }
                return result;
            }
        }
        public List<SellerPayoutReceiptRow> SellerPayoutReceipt(int actor, int payoutId) //Tạo dữ liệu phiếu chi chủ hàng
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "PAYOUTS", "View");
                var result = (from p in db.SellerPayouts
                join c in db.Consignors on p.ConsignorId equals c.ConsignorId
                join l in db.AuctionLots on p.LotId equals l.LotId
                join item in db.Items on l.ItemId equals item.ItemId
                where p.PayoutId == payoutId
                select new SellerPayoutReceiptRow
                {
                    PayoutCode = p.PayoutCode, 
                    ConsignorName = c.FullName, LotNumber = l.LotNumber, ItemName = item.ItemName, 
                    PaidAt = p.PayoutAt, HammerPrice = p.HammerPrice, SellerFee = p.SellerFee, 
                    PayoutAmount = p.PayoutAmount, PaymentMethod = p.PaymentMethod, 
                    TransferReference = p.TransferReference
                }).ToList();
                if (result.Count != 1)
                {
                    throw new BusinessException("Không tìm thấy phiếu chi chủ hàng.");
                }
                return result;
            }
        }
        // Truy vấn từ EF, không sử dụng ADO.NET thuần và không làm tròn trong SQL.
        public List<InvoiceReportRow> Invoice(int actor, int invoiceId) //Tạo dữ liệu in hóa đơn.
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                var rows = (from d in db.InvoiceDetails join h in db.Invoices on d.InvoiceId equals h.InvoiceId
                join c in db.Customers on h.CustomerId equals c.CustomerId
                join a in db.AuctionSessions on h.SessionId equals a.SessionId
                join l in db.AuctionLots on d.LotId equals l.LotId
                join it in db.Items on l.ItemId equals it.ItemId
                where h.InvoiceId == invoiceId
                orderby l.LotNumber
                select new InvoiceReportRow
                {
                    InvoiceCode = h.InvoiceCode, CustomerName = c.FullName, 
                    SessionName = a.SessionName, IssuedAt = h.IssuedAt, ItemName = it.ItemName, HammerPrice = d.HammerPrice, 
                    HammerSubtotal = h.HammerSubtotal, BuyerFee = h.BuyerFee, VipDiscount = h.VipDiscount, 
                    VatAmount = h.VatAmount, DepositApplied = h.DepositApplied, TotalDue = h.TotalDue, PaidAmount = h.PaidAmount
                }).ToList();
                if (rows.Count == 0)
                {
                    throw new InvalidOperationException("Hóa đơn không tồn tại hoặc không có chi tiết.");
                }
                return rows;
            }
        }
        public List<ConsignorReturnReportRow> ReturnToOwner(int actor, int returnId) //Tạo dữ liệu biên bản trả chủ hàng.
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                var rows = (from r in db.ConsignorReturns
                join it in db.Items on r.ItemId equals it.ItemId
                join o in db.Consignors on r.ConsignorId equals o.ConsignorId
                where r.ReturnId == returnId
                select new ConsignorReturnReportRow
                {
                    ReturnCode = r.ReturnCode, 
                    ItemCode = it.ItemCode, ItemName = it.ItemName, ConsignorName = o.FullName, 
                    RecipientName = r.RecipientName, ReturnedAt = r.ReturnedAt, Notes = r.Notes
                }).ToList();
                if (rows.Count == 0)
                {
                    throw new BusinessException("Không tìm thấy biên bản trả hiện vật.");
                }
                return rows;
            }
        }
        public List<ContractReportRow> Contract(int actor, int contractId) //Tạo dữ liệu in hợp đồng.
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "CONTRACTS", "View");
                var rows = (from d in db.ConsignmentContractDetails
                join c in db.ConsignmentContracts on d.ContractId equals c.ContractId
                join o in db.Consignors on c.ConsignorId equals o.ConsignorId
                join i in db.Items on d.ItemId equals i.ItemId
                where d.ContractId == contractId
                orderby i.ItemCode
                select new ContractReportRow
                {
                    ContractCode = c.ContractCode, 
                    OwnerName = o.FullName, SignedAt = c.SignedAt, ItemCode = i.ItemCode, 
                    ItemName = i.ItemName, Status = c.ContractStatus, 
                    SellerFeeRate = c.SellerFeeRate, ExpiresAt = c.ExpiresAt
                }).ToList();
                if (rows.Count == 0)
                {
                    throw new InvalidOperationException("Hợp đồng chưa có hiện vật.");
                }
                return rows;
            }
        }
        public List<IssueReportRow> Issue(int actor, int issueId) //Tạo dữ liệu in phiếu xuất
        {
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                var rows = (from d in db.StockIssueDetails join h in db.StockIssues on d.IssueId equals h.IssueId
                join i in db.Invoices on h.InvoiceId equals i.InvoiceId
                join item in db.Items on d.ItemId equals item.ItemId
                where h.IssueId == issueId orderby item.ItemCode
                select new IssueReportRow
                {
                    IssueCode = h.IssueCode, InvoiceCode = i.InvoiceCode, 
                    RecipientName = h.RecipientName, RecipientDocument = h.RecipientDocument, 
                    RecipientRelation = h.RecipientRelation, DeliveryMethod = h.DeliveryMethod, 
                    ConditionAtDelivery = d.ConditionAtDelivery, LocationAtIssue = d.LocationAtIssue, 
                    IssuedAt = h.IssuedAt, ItemCode = item.ItemCode, ItemName = item.ItemName
                }).ToList();
                if (rows.Count == 0)
                {
                    throw new InvalidOperationException("Không có dữ liệu phiếu xuất kho.");
                }
                return rows;
            }
        }
        public List<RevenueReportRow> Revenue(int actor, DateTime fromDate, DateTime toDate) //Tính doanh thu cho kỳ được chọn(569-675).
        {
            if (fromDate.Date>toDate.Date || toDate.Date == DateTime.MaxValue.Date)
            throw new ArgumentException("Khoảng thời gian báo cáo không hợp lệ.");
            var from = fromDate.Date;
            var endExclusive = toDate.Date.AddDays(1);
            using (var db = new AuctionArenaDbContext())
            {
                SecurityService.Require(db, actor, "REPORTS", "View");
                // Ngày doanh thu là ngày hoàn tất thu tiền, không phải ngày phát hành hóa đơn.
                var paidDates = db.Payments.Where(p => p.InvoiceId.HasValue && 
                (p.PaymentType == "Invoice" || p.PaymentType == "DepositTransfer"))
                .GroupBy(p => p.InvoiceId.Value)
                .Select(g => new
                {
                    InvoiceId = g.Key, LastPaidAt = g.Max(x => x.PaidAt)
                })
                .Where(x => x.LastPaidAt>=from && x.LastPaidAt<endExclusive)
                .ToList();
                var ids = paidDates.Select(x => x.InvoiceId).ToList();
                if (ids.Count == 0)
                {
                    return new List<RevenueReportRow>();
                }
                var paidDateByInvoice = paidDates.ToDictionary(x => x.InvoiceId, x => x.LastPaidAt);
                var raw = (from d in db.InvoiceDetails
                join inv in db.Invoices on d.InvoiceId equals inv.InvoiceId
                join lot in db.AuctionLots on d.LotId equals lot.LotId
                join item in db.Items on lot.ItemId equals item.ItemId
                join category in db.Categories on item.CategoryId equals category.CategoryId
                join session in db.AuctionSessions on inv.SessionId equals session.SessionId
                where ids.Contains(inv.InvoiceId) && inv.InvoiceStatus == "Paid"
                select new
                {
                    inv.InvoiceId, inv.HammerSubtotal, inv.BuyerFee, inv.VipDiscount, 
                    d.HammerPrice, d.LotId, lot.SellerFeeRateLocked, 
                    session.SessionName, category.CategoryName
                }).ToList();
                if (raw.Count == 0)
                {
                    return new List<RevenueReportRow>();
                }
                var lotIds = raw.Select(x => x.LotId).Distinct().ToList();
                var payouts = db.SellerPayouts.Where(x => lotIds.Contains(x.LotId))
                .Select(x => new
                {
                    x.LotId, x.SellerFee
                }).ToList()
                .GroupBy(x => x.LotId).ToDictionary(g => g.Key, g => g.Sum(x => x.SellerFee));
                // Không được báo phí người bán = 0 nếu phí của lô cũ chưa được chốt.
                var missing = raw.Where(x => !payouts.ContainsKey(x.LotId) && !x.SellerFeeRateLocked.HasValue)
                .Select(x => x.LotId).Distinct().ToList();
                if (missing.Count>0)
                throw new BusinessException("Cần xác nhận phí người bán cho lot: "+string.Join(", ", missing)+
                ". Không thể xuất báo cáo doanh thu đầy đủ khi thiếu phí.");
                // Phân bổ phí mua ròng theo từng lô với tổng đúng đến đồng xu.
                // Nếu làm tròn theo nhóm loại hiện vật sẽ có sai lệch 0,01 khi một hóa đơn có nhiều loại.
                var allocatedBuyerFee = new Dictionary<int, decimal>();
                foreach (var invoiceGroup in raw.GroupBy(x => x.InvoiceId))
                {
                    var ordered = invoiceGroup.OrderBy(x => x.LotId).ToList();
                    var header = ordered[0];
                    if (header.HammerSubtotal != ordered.Sum(x => x.HammerPrice))
                    throw new BusinessException("Hóa đơn #"+invoiceGroup.Key+" có tổng giá búa lệch chi tiết. Cần đối soát.");
                    decimal net = header.BuyerFee-header.VipDiscount;
                    if (net<0m)
                    {
                        throw new BusinessException("Hóa đơn có phí người mua ròng âm.");
                    }
                    decimal assigned = 0m;
                    for (int index = 0; index<ordered.Count; index++)
                    {
                        var lot = ordered[index];
                        decimal fee = index == ordered.Count-1 ? net-assigned :
                        (header.HammerSubtotal == 0m ? 0m : Math.Min(net-assigned, 
                        Math.Round(net*lot.HammerPrice/header.HammerSubtotal, 2, MidpointRounding.AwayFromZero)));
                        allocatedBuyerFee.Add(lot.LotId, fee);
                        assigned+=fee;
                    }
                }
                var records = raw.Select(x => new
                {
                    Date = paidDateByInvoice[x.InvoiceId].Date, x.InvoiceId, 
                    x.SessionName, x.CategoryName, x.HammerPrice, 
                    BuyerFee = allocatedBuyerFee[x.LotId], 
                    SellerFee = payouts.ContainsKey(x.LotId)?payouts[x.LotId]:
                    Math.Round(x.HammerPrice*x.SellerFeeRateLocked.Value, 2, MidpointRounding.AwayFromZero)
                }).ToList();
                return records.GroupBy(x => new
                {
                    x.Date, x.SessionName, x.CategoryName
                })
                .Select(g => 
                {
                    var buyer = g.Sum(x => x.BuyerFee);
                    var seller = g.Sum(x => x.SellerFee);
                    return new RevenueReportRow
                    {
                        IssuedAt = g.Key.Date, SessionName = g.Key.SessionName, 
                        CategoryName = g.Key.CategoryName, HammerTotal = g.Sum(x => x.HammerPrice), 
                        BuyerFee = buyer, SellerFee = seller, PlatformRevenue = buyer+seller, 
                        InvoiceCount = g.Select(x => x.InvoiceId).Distinct().Count()
                    };
                })
                .OrderBy(x => x.IssuedAt).ThenBy(x => x.SessionName).ToList();
            }
        }
    }
    public sealed class ItemLabelRow
    {
        public string ItemCode
        {
            get;
            set;
        }
        public string ItemName
        {
            get;
            set;
        }
        public string CategoryName
        {
            get;
            set;
        }
        public string OwnerName
        {
            get;
            set;
        }
        public decimal EstimatePrice
        {
            get;
            set;
        }
        public string ItemStatus
        {
            get;
            set;
        }
        public string Origin
        {
            get;
            set;
        }
        public string Era
        {
            get;
            set;
        }
    }
    public sealed class ItemLabelReportService : ServiceBase
    {
        public ItemLabelReportService(Func<AuctionArenaDbContext> factory = null):base(factory)
        {
        }
        public List<ItemLabelRow> Get(int actor, int id) //Lấy dữ liệu theo điều kiện của Service
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ITEMS", "View");
                return (from i in db.Items join c in db.Categories on i.CategoryId equals c.CategoryId
                join o in db.Consignors on i.ConsignorId equals o.ConsignorId
                where i.ItemId == id select new ItemLabelRow
                {
                    ItemCode = i.ItemCode, ItemName = i.ItemName, CategoryName = c.CategoryName, 
                    OwnerName = o.FullName, EstimatePrice = i.EstimatePrice, 
                    ItemStatus = i.ItemStatus, Origin = i.Origin, Era = i.Era
                }).ToList();
            }
        }
        public bool CanPrint(int actor) //Kiểm tra quyền in phân hệ.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ITEMS", "Print");
                return true;
            }
        }
    }
}
