using System;
using System.Data.Entity;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
namespace AuctionArena.Services
{
    public sealed class RegistrationService : ServiceBase
    {
        public RegistrationService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public int Register(int actor, int sessionId, int customerId, int paddleNumber) //Đăng ký khách và xác lập nghĩa vụ tiền cọc.
        {
            if (paddleNumber <= 0)
            {
                throw new BusinessException("Số paddle phải lớn hơn 0.");
            }
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "REGISTRATIONS", "Create");
                    var session = db.AuctionSessions.Single(x => x.SessionId == sessionId);
                    if (session.SessionStatus != "Open" && session.SessionStatus != "Planned")
                    throw new BusinessException("Phiên không cho đăng ký.");
                    var customer = db.Customers.Single(x => x.CustomerId == customerId);
                    // Hồ sơ chưa xác minh CCCD/giấy tờ và liên hệ không được cấp paddle.
                    // Khách chưa có giấy tờ hợp lệ phải bổ sung hồ sơ trước khi đăng ký.
                    if (string.IsNullOrWhiteSpace(customer.IdentityNumber) || 
                    string.IsNullOrWhiteSpace(customer.Phone))
                    throw new BusinessException("Khách thiếu giấy tờ định danh hoặc số điện thoại. Hãy hoàn thiện hồ sơ trước khi cấp paddle.");
                    if (customer.CustomerStatus == "Inactive" || customer.IsBlacklisted || customer.HasOutstandingDebt || 
                    db.Invoices.Any(x => x.CustomerId == customerId && x.PaidAmount < x.TotalDue && x.DueAt < DateTime.Now && x.InvoiceStatus != "Cancelled"))
                    throw new BusinessException("Khách bị chặn hoặc đang nợ quá hạn.");
                    if (db.Registrations.Any(x => x.SessionId == sessionId && (x.CustomerId == customerId || x.PaddleNumber == paddleNumber)))
                    throw new BusinessException("Khách đã đăng ký hoặc số paddle đã có người dùng.");
                    // Một người thật không được lấy hai paddle bằng hai mã khách có cùng giấy tờ.
                    var priorDocuments = (from r in db.Registrations join c in db.Customers on r.CustomerId equals c.CustomerId
                    where r.SessionId == sessionId select c.IdentityNumber).ToList();
                    if (priorDocuments.Any(x => string.Equals((x??"").Trim(), customer.IdentityNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
                    throw new BusinessException("Giấy tờ định danh này đã có paddle trong phiên; không được đăng ký trùng người.");
                    var sessionLots = db.AuctionLots.Where(x => x.SessionId == sessionId && 
                    x.LotStatus != "Cancelled").Select(x => new
                    {
                        x.StartingPrice, x.DepositRequired
                    }).ToList();
                    if (sessionLots.Count == 0)
                    throw new BusinessException("Phiên chưa có lot. Hãy thiết lập hiện vật và mức cọc trước khi đăng ký khách.");
                    decimal fallbackRate = RateSetting(db, "DepositRate");
                    decimal required = sessionLots.Max(x => x.DepositRequired ??
                    Math.Round(x.StartingPrice*fallbackRate, 2, MidpointRounding.AwayFromZero));
                    if (required <= 0m)
                    throw new BusinessException("Tiền cọc tính được bằng 0. Hãy kiểm tra giá khởi điểm và tỷ lệ cọc.");
                    var reg = new RegistrationEntity
                    {
                        SessionId = sessionId, CustomerId = customerId, PaddleNumber = paddleNumber, 
                        DepositRequired = required, DepositPaid = 0, DepositApplied = 0, DepositRefunded = 0, DepositForfeited = 0, 
                        RegistrationStatus = "Pending", RegisteredAt = DateTime.Now
                    };
                    db.Registrations.Add(reg);
                    db.SaveChanges();
                    Audit(db, actor, "REGISTER", "Registrations", reg.RegistrationId, "Đăng ký paddle, chờ đủ cọc");
                    db.SaveChanges();
                    tx.Commit();
                    return reg.RegistrationId;
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Dữ liệu đăng ký đã thay đổi.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
    // Đối soát hóa đơn đã thu một phần và quá hạn; không xóa, tự tịch thu hoặc đảo chứng từ.
    public sealed class InvoiceReviewService : ServiceBase
    {
        public InvoiceReviewService(Func<AuctionArenaDbContext> factory = null):base(factory)
        {
        }
        public int OpenPartialOverdueReview(int actor, int invoiceId, string reason) //Mở hồ sơ đối soát hóa đơn quá hạn đã thu một phần.
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length<10 || reason.Trim().Length>300)
            throw new BusinessException("Ghi rõ căn cứ đối soát (10–300 ký tự).");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Edit");
                    var invoice = db.Invoices.SingleOrDefault(x => x.InvoiceId == invoiceId);
                    if (invoice == null || invoice.DueAt>=Now() || invoice.InvoiceStatus == "Cancelled" || 
                    invoice.InvoiceStatus == "Paid" || invoice.PaidAmount<=0m || 
                    invoice.PaidAmount>=invoice.TotalDue)
                    throw new BusinessException("Chỉ tạo biên bản đối soát cho hóa đơn quá hạn đã thu một phần.");
                    if (db.InvoiceReviewCases.Any(x => x.InvoiceId == invoiceId && x.ReviewStatus == "Open"))
                    throw new BusinessException("Hóa đơn đã có biên bản đối soát chưa giải quyết.");
                    var actual = db.Payments.Where(x => x.InvoiceId == invoiceId && x.PaymentType == "Invoice")
                    .Select(x => (decimal?)x.Amount).Sum() ?? 0m;
                    if (actual != invoice.PaidAmount)
                    throw new BusinessException("Sổ thu tiền không khớp công nợ; cần kiểm tra chứng từ trước.");
                    var review = new InvoiceReviewCaseEntity
                    {
                        InvoiceId = invoiceId, 
                        RemainingAtReview = invoice.TotalDue-invoice.PaidAmount, 
                        ReviewReason = reason.Trim(), ReviewStatus = "Open", CreatedAt = Now(), CreatedByUserId = actor
                    };
                    db.InvoiceReviewCases.Add(review);
                    db.SaveChanges();
                    Audit(db, actor, "OPEN_OVERDUE_REVIEW", "InvoiceReviewCases", review.ReviewId, 
                    "Đối soát hóa đơn "+invoice.InvoiceCode+"; còn nợ "+review.RemainingAtReview.ToString("N0"));
                    db.SaveChanges();
                    tx.Commit();
                    return review.ReviewId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
    public sealed class SessionService : ServiceBase
    {
        public SessionService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public void CloseSession(int actor, int sessionId) //Đóng phiên sau kiểm tra điều kiện(135-179)
        {
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "SESSIONS", "Edit");
                    var session = db.AuctionSessions.Single(x => x.SessionId == sessionId);
                    if (session.SessionStatus != "Open")
                    {
                        throw new BusinessException("Phiên không đang mở.");
                    }
                    if (db.AuctionLots.Any(x => x.SessionId == sessionId && (x.LotStatus == "Open" || x.LotStatus == "Planned")))
                    throw new BusinessException("Còn lot chưa chốt, không thể kết thúc phiên.");
                    session.SessionStatus = "Closed";
                    // Tạo nghĩa vụ phải hoàn cho khách không thắng; chưa ghi nhận đã chi tiền.
                    var winners = db.AuctionLots.Where(x => x.SessionId == sessionId && x.LotStatus == "Sold" && 
                    x.WinningCustomerId.HasValue).Select(x => x.WinningCustomerId.Value).Distinct().ToList();
                    var registrations = db.Registrations.Where(x => x.SessionId == sessionId).ToList();
                    foreach (var reg in registrations.Where(x => !winners.Contains(x.CustomerId)))
                    {
                        decimal due = reg.DepositPaid-reg.DepositApplied-reg.DepositRefunded-reg.DepositForfeited;
                        if (due<0m)throw new BusinessException("Có số dư cọc âm; không được kết thúc phiên trước khi đối soát.");
                        if (due<=0m)continue;
                        if (db.DepositRefundObligations.Any(x => x.RegistrationId == reg.RegistrationId))
                        throw new BusinessException("Đăng ký có nghĩa vụ hoàn cọc đã tồn tại; cần đối soát.");
                        db.DepositRefundObligations.Add(new DepositRefundObligationEntity
                        {
                            SessionId = sessionId, RegistrationId = reg.RegistrationId, DueAmount = due, 
                            ObligationStatus = "Pending", CreatedAt = Now()
                        });
                    }
                    Audit(db, actor, "CLOSE_SESSION", "AuctionSessions", sessionId, 
                    "Kết thúc phiên; tạo danh sách nghĩa vụ hoàn cọc chưa thực chi");
                    db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Chỉ dùng cho hóa đơn đến hạn, chưa thanh toán. Không tự giao cho người trả thứ hai.
        public void ForfeitOverdueDeposit(int actor, int invoiceId) //Xử lý tịch thu cọc theo điều kiện nghiệp vụ(181-230_)
        {
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Edit");
                    var invoice = db.Invoices.Single(x => x.InvoiceId == invoiceId);
                    if (invoice.InvoiceStatus == "Paid" || invoice.InvoiceStatus == "Cancelled" || invoice.InvoiceStatus == "Overdue" || invoice.DueAt >= DateTime.Now || invoice.PaidAmount >= invoice.TotalDue)
                    throw new BusinessException("Hóa đơn chưa quá hạn, đã hoàn tất hoặc đã ghi nhận quá hạn.");
                    if (invoice.PaidAmount > 0m || 
                    db.Payments.Any(x => x.InvoiceId == invoiceId && x.PaymentType == "Invoice"))
                    throw new BusinessException("Hóa đơn đã có tiền thu thực tế. Không tự tịch thu cọc; cần kế toán đối soát và lập phương án xử lý riêng.");
                    var reg = db.Registrations.Single(x => x.SessionId == invoice.SessionId && x.CustomerId == invoice.CustomerId);
                    // Cọc đã chuyển vào hóa đơn không được tịch thu lần hai.
                    decimal available = reg.DepositPaid - reg.DepositApplied - reg.DepositRefunded - reg.DepositForfeited;
                    if (available < 0m)
                    {
                        throw new BusinessException("Số dư tiền cọc đang sai, phải đối soát trước khi xử lý quá hạn.");
                    }
                    if (available > 0)
                    {
                        reg.DepositForfeited += available;
                        db.Payments.Add(new PaymentEntity
                        {
                            PaymentCode = Code("PT"), RegistrationId = reg.RegistrationId, 
                            PaymentType = "Forfeit", Amount = available, PaymentMethod = "Internal", PaidAt = DateTime.Now, 
                            ReceivedByUserId = actor, Notes = "Tịch thu phần cọc còn lại vì quá hạn"
                        });
                    }
                    invoice.InvoiceStatus = "Overdue";
                    db.Customers.Single(x => x.CustomerId == invoice.CustomerId).HasOutstandingDebt = true;
                    db.CustomerViolations.Add(new CustomerViolationEntity
                    {
                        CustomerId = invoice.CustomerId, 
                        SessionId = invoice.SessionId, ViolationType = "NonPayment", Description = "Thanh toán quá hạn hóa đơn " + invoice.InvoiceCode, 
                        OccurredAt = DateTime.Now
                    });
                    Audit(db, actor, "PAYMENT_OVERDUE", "Invoices", invoiceId, "Ghi nhận quá hạn và phần cọc còn lại bị tịch thu");
                    db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
    // Dữ liệu xem trước số tiền thực chi, không thay đổi database.
    public sealed class RefundPreview
    {
        public int CustomerCount
        {
            get;
            set;
        }
        public decimal TotalAmount
        {
            get;
            set;
        }
    }
    public sealed class DepositSettlementService : ServiceBase
    {
        public DepositSettlementService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public RefundPreview PreviewLoserRefunds(int actor, int sessionId) //Xem trước các khoản hoàn cọc khách không trúng(251-273).
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REGISTRATIONS", "Edit");
                VerifyClosed(db, sessionId);
                var winnerIds = db.AuctionLots.Where(l => l.SessionId == sessionId && 
                l.LotStatus == "Sold" && l.WinningCustomerId.HasValue)
                .Select(l => l.WinningCustomerId.Value).Distinct().ToList();
                var regs = db.Registrations.Where(r => r.SessionId == sessionId).ToList();
                var amounts = regs.Where(r => !winnerIds.Contains(r.CustomerId))
                .Select(r => r.DepositPaid - r.DepositApplied - r.DepositRefunded - r.DepositForfeited).ToList();
                if (amounts.Any(a => a < 0m))
                {
                    throw new BusinessException("Số dư cọc âm, cần đối soát trước khi hoàn.");
                }
                return new RefundPreview
                {
                    CustomerCount = amounts.Count(a => a > 0m), 
                    TotalAmount = amounts.Where(a => a > 0m).Sum()
                };
            }
        }
        // Chỉ gọi sau khi nhân viên xác nhận đã hoàn tiền mặt cho khách không trúng.
        // Mọi phiếu hoàn và số dư cọc được ghi trong cùng một transaction.
        public RefundPreview RefundLosersByCash(int actor, int sessionId) //Ghi hoàn cọc tiền mặt theo danh sách(276-340)
        {
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "REGISTRATIONS", "Edit");
                    VerifyClosed(db, sessionId);
                    var winnerIds = db.AuctionLots.Where(l => l.SessionId == sessionId && 
                    l.LotStatus == "Sold" && l.WinningCustomerId.HasValue)
                    .Select(l => l.WinningCustomerId.Value).Distinct().ToList();
                    var regs = db.Registrations.Where(r => r.SessionId == sessionId)
                    .OrderBy(r => r.RegistrationId).ToList();
                    var result = new RefundPreview();
                    foreach (var reg in regs.Where(r => !winnerIds.Contains(r.CustomerId)))
                    {
                        decimal amount = reg.DepositPaid - reg.DepositApplied -
                        reg.DepositRefunded - reg.DepositForfeited;
                        if (amount < 0m)
                        {
                            throw new BusinessException("Số dư cọc âm, hủy toàn bộ thao tác.");
                        }
                        if (amount == 0m) continue;
                        // Đã hoàn, chưa cọc hoặc không còn dư.
                        reg.DepositRefunded += amount;
                        reg.RegistrationStatus = "Closed";
                        var obligation = db.DepositRefundObligations.SingleOrDefault(x => x.RegistrationId == reg.RegistrationId);
                        if (obligation != null)
                        {
                            if (obligation.ObligationStatus != "Pending" || obligation.DueAmount != amount)
                            throw new BusinessException("Số dư cọc khác phiếu chờ hoàn; cần đối soát trước khi chi.");
                            obligation.ObligationStatus = "Settled";
                            obligation.SettledAt = Now();
                        }
                        db.Payments.Add(new PaymentEntity
                        {
                            PaymentCode = Code("PT"), RegistrationId = reg.RegistrationId, 
                            PaymentType = "Refund", Amount = amount, PaymentMethod = "Cash", 
                            PaidAt = Now(), ReceivedByUserId = actor, 
                            Notes = "Hoàn cọc khách không thắng - đã chi tiền mặt khi xác nhận hàng loạt"
                        });
                        Audit(db, actor, "REFUND_DEPOSIT", "Registrations", reg.RegistrationId, 
                        "Hoàn cọc tiền mặt " + amount.ToString("N0"));
                        result.CustomerCount++;
                        result.TotalAmount += amount;
                    }
                    if (result.CustomerCount == 0)
                    throw new BusinessException("Không còn khoản cọc nào cần hoàn cho khách không thắng.");
                    db.SaveChanges();
                    tx.Commit();
                    return result;
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Số dư cọc vừa thay đổi tại máy khác, hãy kiểm tra lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        private static void VerifyClosed(AuctionArenaDbContext db, int sessionId) //Kiểm tra phiên đã đóng.

        {
            var session = db.AuctionSessions.SingleOrDefault(s => s.SessionId == sessionId);
            if (session == null || session.SessionStatus != "Closed")
            throw new BusinessException("Chỉ hoàn cọc hàng loạt sau khi phiên đã kết thúc.");
            if (db.AuctionLots.Any(l => l.SessionId == sessionId && 
            (l.LotStatus == "Open" || l.LotStatus == "Planned")))
            throw new BusinessException("Phiên còn lot chưa chốt.");
        }
    }
}
