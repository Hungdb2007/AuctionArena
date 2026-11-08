using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
namespace AuctionArena.Services
{
    public sealed class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }
    }
    // Kiểm tra quyền tại Service trước mỗi thao tác ghi.
    public sealed class SecurityService
    {
        public static void Require(AuctionArenaDbContext db, int userId, string permission, string action)
        {//Chặn thao tác nếu tài khoản/nhóm/quyền không hợp lệ.
            var user = db.Users.SingleOrDefault(x => x.UserId == userId && x.IsActive);
            if (user == null || !db.Roles.Any(r => r.RoleId == user.RoleId && r.IsActive))
            {
                throw new BusinessException("Phiên đăng nhập không hợp lệ.");
            }
            var allowed = (from rp in db.RolePermissions
            join p in db.Permissions on rp.PermissionId equals p.PermissionId
            where rp.RoleId == user.RoleId && p.PermissionCode == permission
            select rp).FirstOrDefault();
            if (allowed == null || !(action == "View" && allowed.CanView || 
            action == "Create" && allowed.CanCreate || action == "Edit" && allowed.CanEdit || 
            action == "Delete" && allowed.CanDelete || action == "Print" && allowed.CanPrint))
            throw new BusinessException("Tài khoản không có quyền thực hiện thao tác này.");
        }
    }
    public abstract class ServiceBase
    {
        protected readonly Func<AuctionArenaDbContext> Factory;
        protected ServiceBase(Func<AuctionArenaDbContext> factory = null)//Khởi tạo ServiceBase. Nhận phụ thuộc/cấu hình ban đầu.
        { 
            Factory = factory ?? (() => new AuctionArenaDbContext());
        }
        protected static decimal Setting(AuctionArenaDbContext db, string key) //Đọc và kiểm tra tham số số từ SystemSettings
        {
            var value = db.SystemSettings.Where(s => s.SettingKey == key).Select(s => s.SettingValue).SingleOrDefault();
            decimal parsed;
            if (value == null || !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed) || parsed < 0)
            throw new BusinessException("Thiếu hoặc sai tham số hệ thống: " + key);
            return parsed;
        }
        // Tỷ lệ phí lưu dưới dạng thập phân, ví dụ 0,10 tương ứng 10%.
        // Tỷ lệ phí phải nằm trong khoảng từ 0 đến 1.
        protected static decimal RateSetting(AuctionArenaDbContext db, string key) //Kiểm tra tỷ lệ nằm trong khoảng 0 đến 1
        {
            var value = Setting(db, key);
            if (value > 1m)
            {
                throw new BusinessException("Tham số tỷ lệ vượt 100%: " + key);
            }
            return value;
        }
        protected static DateTime Now()//Lấy thời điểm nghiệp vụ hiện tại.
        {
            return DateTime.Now;
        }
        // SQL Server có thể đã tự hủy giao dịch khi lỗi ràng buộc/trigger xảy ra.
        // Không để lỗi rollback lần hai che mất lỗi gốc; using vẫn giải phóng giao dịch.
        protected static void RollbackSafely(DbContextTransaction transaction)
        {//Rollback mà không che mất lỗi gốc nếu giao dịch đã bị SQL hủy.
            try
            {
                if (transaction.UnderlyingTransaction.Connection != null)
                    transaction.Rollback();
            }
            catch (Exception rollbackError)
            {
                System.Diagnostics.Trace.TraceError("Không thể rollback giao dịch: " + rollbackError.Message);
            }
        }
        protected static string Code(string prefix) //Sinh mã chứng từ từ Guid.
        {
            return prefix + Guid.NewGuid().ToString("N").Substring(0, 18).ToUpperInvariant();
        }
        protected static void Audit(AuctionArenaDbContext db, int user, string action, string entity, int id, string description)
        { //Thêm nhật ký trong context hiện hành.
            db.AuditLogs.Add(new AuditLogEntity
            {
                UserId = user, ActionCode = action, EntityName = entity, 
                EntityId = id.ToString(CultureInfo.InvariantCulture), Description = description, OccurredAt = Now()
            });
        }
        protected static void ValidateMoney(decimal amount) //Kiểm tra tiền dương và đúng độ chính xác
        {
            ValidateMoneyOrZero(amount);
            if (amount == 0m)
            {
                throw new BusinessException("Số tiền phải lớn hơn 0.");
            }
        }
        protected static void ValidateMoneyOrZero(decimal amount) //Kiểm tra tiền không âm, tối đa hai chữ số thập phân
        {
            if (amount < 0m || amount > 99999999999999999.99m || 
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new BusinessException("Số tiền phải không âm, trong giới hạn DECIMAL(19,2) và tối đa 2 chữ số thập phân.");
        }
    }
    // Kiểm tra điều kiện của lô trước khi nhận hoặc chốt giá.
    // Giá trả và trạng thái lô được lưu trong cùng transaction.
    public sealed class AuctionService : ServiceBase
    {
        public AuctionService(Func<AuctionArenaDbContext> factory = null) : base(factory)//Khởi tạo AuctionService. Nhận phụ thuộc/cấu hình ban đầu.
        {
        }
        public void Bid(int actor, int lotId, int customerId, decimal amount) //Kiểm tra cọc/quyền/giá rồi lưu lượt trả giá và cập nhật lô ( 114-194)
        {
            ValidateMoney(amount);
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "BIDS", "Create");
                    var lot = db.AuctionLots.SingleOrDefault(x => x.LotId == lotId);
                    if (lot == null)
                    {
                        throw new BusinessException("Không tìm thấy lot.");
                    }
                    var session = db.AuctionSessions.Single(x => x.SessionId == lot.SessionId);
                    var now = Now();
                    if (session.SessionStatus != "Open" || lot.LotStatus != "Open" || now < lot.OpensAt || now >= lot.ClosesAt)
                    throw new BusinessException("Phiên hoặc lot chưa mở hay đã hết thời gian.");
                    var customer = db.Customers.SingleOrDefault(x => x.CustomerId == customerId);
                    if (customer == null || customer.CustomerStatus == "Inactive" || customer.IsBlacklisted || customer.HasOutstandingDebt || 
                    db.Invoices.Any(x => x.CustomerId == customerId && (x.InvoiceStatus == "Overdue" || 
                    ((x.InvoiceStatus == "Unpaid" || x.InvoiceStatus == "PartiallyPaid") && x.DueAt < now))))
                    throw new BusinessException("Khách không đủ điều kiện tham gia đấu giá.");
                    var reg = db.Registrations.SingleOrDefault(x => x.SessionId == lot.SessionId && x.CustomerId == customerId);
                    if (reg == null || reg.RegistrationStatus != "Active" || reg.DepositPaid < reg.DepositRequired || reg.PaddleNumber <= 0)
                    throw new BusinessException("Khách chưa đăng ký, đặt cọc đủ hoặc chưa được cấp paddle.");
                    // Kiểm tra giấy tờ trùng giữa chủ hàng và khách tham gia.
                    var otherDocuments = (from r in db.Registrations join c in db.Customers on r.CustomerId equals c.CustomerId
                    where r.SessionId == lot.SessionId && r.CustomerId != customerId
                    select c.IdentityNumber).ToList();
                    if (!string.IsNullOrWhiteSpace(customer.IdentityNumber) && 
                    otherDocuments.Any(x => string.Equals((x??"").Trim(), customer.IdentityNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
                    throw new BusinessException("Phiên có hai paddle trùng giấy tờ định danh. Cần xử lý hồ sơ khách trước khi ghi giá.");
                    var item = db.Items.Single(x => x.ItemId == lot.ItemId);
                    var seller = db.Consignors.Single(x => x.ConsignorId == item.ConsignorId);
                    // Không nhận giá nếu thiếu giấy tờ để đối chiếu chủ hàng.
                    if (string.IsNullOrWhiteSpace(customer.IdentityNumber) || string.IsNullOrWhiteSpace(seller.IdentityNumber))
                    throw new BusinessException("Thiếu giấy tờ định danh của khách hoặc chủ hàng; phải xác minh hồ sơ trước khi ghi giá.");
                    if (string.Equals(seller.IdentityNumber.Trim(), customer.IdentityNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new BusinessException("Chủ hàng không được trả giá cho hiện vật của mình.");
                    if (lot.LeadingCustomerId == customerId)
                    {
                        throw new BusinessException("Khách đang dẫn đầu không được tự nâng giá.");
                    }
                    // Lượt đầu: khách có thể trả đúng giá khởi điểm.
                    // Các lượt sau: phải cao hơn giá hiện tại ít nhất một bước giá.
                    bool daCoLuotTraGia = db.Bids.Any(x => x.LotId == lotId);
                    decimal minimum = daCoLuotTraGia
                    ? lot.CurrentPrice + lot.BidIncrement
                    : lot.StartingPrice;
                    if (amount < minimum)
                    {
                        throw new BusinessException("Giá trả thấp hơn giá tối thiểu: " + minimum.ToString("N0"));
                    }
                    db.Bids.Add(new BidEntity
                    {
                        LotId = lotId, CustomerId = customerId, RegistrationId = reg.RegistrationId, 
                        Amount = amount, BidAt = now, EnteredByUserId = actor
                    });
                    lot.CurrentPrice = amount;
                    lot.LeadingCustomerId = customerId;
                    int extension = checked((int)Setting(db, "AntiSnipingSeconds"));
                    if (extension > 0 && (lot.ClosesAt - now).TotalSeconds <= extension)
                    lot.ClosesAt = now.AddSeconds(extension);
                    Audit(db, actor, "BID", "AuctionLots", lotId, "Ghi giá trả " + amount.ToString("N0"));
                    db.SaveChanges();
                    // Dùng RowVersion để phát hiện lô vừa được máy khác cập nhật.
                    tx.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Giá vừa thay đổi tại máy khác. Hãy tải lại lot rồi trả giá lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Chỉ chốt đúng trạng thái nhân viên đã nhìn thấy và xác nhận.
        // Nếu RowVersion thay đổi thì tải lại lô trước khi chốt.
        public void CloseLot(int actor, int lotId, byte[] confirmedRowVersion) //Chốt lô dựa trên giá, người dẫn đầu và RowVersion xác nhận(197-245).
        {
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "LOTS", "Edit");
                    var lot = db.AuctionLots.SingleOrDefault(x => x.LotId == lotId);
                    if (lot == null || lot.LotStatus != "Open")
                    {
                        throw new BusinessException("Lot không ở trạng thái đang mở.");
                    }
                    if (confirmedRowVersion == null || lot.RowVersion == null || 
                    !lot.RowVersion.SequenceEqual(confirmedRowVersion))
                    throw new BusinessException("Giá lot đã thay đổi từ lúc xác nhận chốt. Hãy kiểm tra người dẫn đầu và giá búa lại.");
                    var session = db.AuctionSessions.Single(x => x.SessionId == lot.SessionId);
                    if (session.SessionStatus != "Open")
                    {
                        throw new BusinessException("Phiên không đang mở.");
                    }
                    if (lot.LeadingCustomerId.HasValue && lot.CurrentPrice >= lot.ReservePrice)
                    {
                        lot.LotStatus = "Sold";
                        lot.HammerPrice = lot.CurrentPrice;
                        lot.WinningCustomerId = lot.LeadingCustomerId;
                        db.Items.Single(x => x.ItemId == lot.ItemId).ItemStatus = "Sold";
                    }
                    else
                    {
                        lot.LotStatus = "ReserveNotMet";
                        db.Items.Single(x => x.ItemId == lot.ItemId).ItemStatus = "Unsold";
                    }
                    Audit(db, actor, "CLOSE_LOT", "AuctionLots", lotId, "Chốt lot: " + lot.LotStatus);
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Lot vừa thay đổi tại máy khác.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
    public sealed class BillingService : ServiceBase
    {
        public BillingService(Func<AuctionArenaDbContext> factory = null) : base(factory) //Khởi tạo BillingService. Nhận phụ thuộc/cấu hình ban đầ
        {
        }
        // Tổng phải trả = Tổng giá búa + phí mua - giảm VIP + VAT trên phí ròng - cọc.
        public int CreateInvoice(int actor, int sessionId, int customerId) //Lập đầu/chi tiết hóa đơn, tính phí và chuyển cọc trong cùng giao dịch(253-335).
        {
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Create");
                    if (db.Invoices.Any(x => x.SessionId == sessionId && x.CustomerId == customerId))
                    throw new BusinessException("Khách đã có hóa đơn của phiên này.");
                    var session = db.AuctionSessions.Single(x => x.SessionId == sessionId);
                    if (session.SessionStatus != "Closed")
                    {
                        throw new BusinessException("Chỉ lập hóa đơn khi phiên đã kết thúc.");
                    }
                    var customer = db.Customers.Single(x => x.CustomerId == customerId);
                    var lots = db.AuctionLots.Where(x => x.SessionId == sessionId && x.WinningCustomerId == customerId && x.LotStatus == "Sold").OrderBy(x => x.LotId).ToList();
                    if (lots.Count == 0)
                    {
                        throw new BusinessException("Khách chưa thắng lot nào.");
                    }
                    var reg = db.Registrations.Single(x => x.SessionId == sessionId && x.CustomerId == customerId);
                    decimal subtotal = lots.Sum(x => x.HammerPrice.Value);
                    decimal feeRate = RateSetting(db, "BuyerFeeRate"), vatRate = RateSetting(db, "VatRate");
                    // Lưu tỷ lệ giảm phí tại thời điểm lập hóa đơn.
                    decimal vipRate = customer.MemberTier == "VIP"?RateSetting(db, "VipDiscountRate"):
                    customer.MemberTier == "Silver"?RateSetting(db, "SilverDiscountRate"):
                    customer.MemberTier == "Gold"?RateSetting(db, "GoldDiscountRate"):0m;
                    decimal fee = Math.Round(subtotal * feeRate, 2, MidpointRounding.AwayFromZero);
                    decimal discount = Math.Round(fee * vipRate, 2, MidpointRounding.AwayFromZero);
                    decimal vat = Math.Round((fee - discount) * vatRate, 2, MidpointRounding.AwayFromZero);
                    decimal available = reg.DepositPaid - reg.DepositApplied - reg.DepositRefunded - reg.DepositForfeited;
                    if (available < 0)
                    {
                        throw new BusinessException("Số dư tiền cọc không hợp lệ, cần đối soát.");
                    }
                    decimal deposit = Math.Min(Math.Max(0, available), subtotal + fee - discount + vat);
                    decimal due = subtotal + fee - discount + vat - deposit;
                    var invoice = new InvoiceEntity
                    {
                        InvoiceCode = Code("HD"), SessionId = sessionId, CustomerId = customerId, 
                        // Hạn thanh toán tính từ lúc kết thúc phiên,
                        // không tính từ ngày phát hành hóa đơn.
                        IssuedAt = Now(), DueAt = session.EndsAt.AddDays((double)Setting(db, "PaymentDeadlineDays")), 
                        InvoiceStatus = due == 0 ? "Paid" : "Unpaid", HammerSubtotal = subtotal, 
                        BuyerFee = fee, VipDiscount = discount, VatAmount = vat, DepositApplied = deposit, 
                        TotalDue = due, PaidAmount = 0, BuyerFeeRate = feeRate, VatRate = vatRate, VipDiscountRate = vipRate
                    };
                    db.Invoices.Add(invoice);
                    db.SaveChanges();
                    foreach (var lot in lots)
                    db.InvoiceDetails.Add(new InvoiceDetailEntity
                    {
                        InvoiceId = invoice.InvoiceId, LotId = lot.LotId, HammerPrice = lot.HammerPrice.Value
                    });
                    if (deposit > 0)
                    {
                        reg.DepositApplied += deposit;
                        db.Payments.Add(new PaymentEntity
                        {
                            PaymentCode = Code("PT"), InvoiceId = invoice.InvoiceId, 
                            RegistrationId = reg.RegistrationId, PaymentType = "DepositTransfer", Amount = deposit, 
                            PaymentMethod = "Internal", PaidAt = Now(), ReceivedByUserId = actor, 
                            Notes = "Chuyển tiền cọc sang hóa đơn, không phải thu tiền mới"
                        });
                    }
                    Audit(db, actor, "CREATE_INVOICE", "Invoices", invoice.InvoiceId, "Lập hóa đơn gộp " + lots.Count + " lot");
                    db.SaveChanges();
                    tx.Commit();
                    return invoice.InvoiceId;
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Dữ liệu cọc hoặc hóa đơn vừa thay đổi. Hãy thử lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Chỉ hủy hóa đơn chưa có thu tiền, chưa chuyển cọc, chưa có phiếu kho.
        // Giữ số hóa đơn và chi tiết để bảo toàn lịch sử, không tạo lại chứng từ trùng.
        public void CancelUncollected(int actor, int invoiceId, string reason) //Hủy hóa đơn chưa thu khi đủ điều kiện (338-370).
        {
            reason = (reason??"").Trim();
            if (reason.Length<10 || reason.Length>250)
            {
                throw new BusinessException("Nhập lý do hủy hóa đơn từ 10 đến 250 ký tự.");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Edit");
                    var invoice = db.Invoices.SingleOrDefault(x => x.InvoiceId == invoiceId);
                    if (invoice == null || invoice.InvoiceStatus != "Unpaid" || invoice.PaidAmount != 0m || invoice.DepositApplied != 0m)
                    throw new BusinessException("Chỉ hủy hóa đơn Chưa thu tiền và Chưa chuyển cọc. Hóa đơn có cọc yêu cầu đối soát.");
                    if (db.Payments.Any(x => x.InvoiceId == invoiceId) || db.StockIssues.Any(x => x.InvoiceId == invoiceId) || 
                    db.InvoiceReviewCases.Any(x => x.InvoiceId == invoiceId) || db.InvoiceAdjustments.Any(x => x.InvoiceId == invoiceId))
                    throw new BusinessException("Đã có giao dịch hoặc hồ sơ đối soát nên không được hủy trực tiếp.");
                    var lots = db.InvoiceDetails.Where(x => x.InvoiceId == invoiceId).Select(x => x.LotId).ToList();
                    if (db.SellerPayoutOrders.Any(x => lots.Contains(x.LotId)) || db.SellerPayouts.Any(x => lots.Contains(x.LotId)))
                    throw new BusinessException("Đã phát sinh chi trả chủ hàng, không được hủy.");
                    invoice.InvoiceStatus = "Cancelled";
                    Audit(db, actor, "CANCEL_UNCOLLECTED_INVOICE", "Invoices", invoiceId, "Unpaid -> Cancelled; "+reason);
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
        // Chỉ thu muộn khi chưa có quyết định tịch thu tiền cọc.
        // Không thu muộn với hóa đơn đã xử lý vi phạm hoặc điều chỉnh.
        // Ghi lại lý do và người duyệt khoản thanh toán quá hạn.
        public void PayLateInvoiceWithApproval(int actor, int invoiceId, decimal amount, string method, string approvalReason)
        { //Thu quá hạn qua luồng có phê duyệt và lý do(374-440)
            ValidateMoney(amount);
            if (method != "Cash" && method != "BankTransfer")
            throw new BusinessException("Hình thức thu không hợp lệ.");
            approvalReason = (approvalReason ?? "").Trim();
            if (approvalReason.Length < 10 || approvalReason.Length > 180)
            throw new BusinessException("Cần ghi nội dung phê duyệt thu trễ từ 10 đến 180 ký tự (mã biên bản/người phê duyệt).");
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Edit");
                    SecurityService.Require(db, actor, "SESSIONS", "Edit");
                    // Quyền quản lý phiên.
                    var invoice = db.Invoices.SingleOrDefault(x => x.InvoiceId == invoiceId);
                    if (invoice == null || (invoice.InvoiceStatus != "Unpaid" && invoice.InvoiceStatus != "PartiallyPaid") || 
                    invoice.DueAt >= Now() || invoice.TotalDue <= invoice.PaidAmount)
                    throw new BusinessException("Chỉ được thu trễ hóa đơn chưa tất toán và chưa ghi nhận vi phạm tịch thu cọc.");
                    var reg = db.Registrations.SingleOrDefault(x => x.SessionId == invoice.SessionId && x.CustomerId == invoice.CustomerId);
                    if (reg == null || reg.DepositForfeited != 0m || reg.DepositApplied != invoice.DepositApplied || 
                    db.InvoiceAdjustments.Any(x => x.InvoiceId == invoiceId) || 
                    db.StockIssues.Any(x => x.InvoiceId == invoiceId) || 
                    db.Payments.Any(x => x.InvoiceId == invoiceId && x.PaymentType == "Forfeit"))
                    throw new BusinessException("Cọc/chứng từ đã có thay đổi hoặc tịch thu. Cần đối soát thủ công, không được thu theo đường này.");
                    var payments = db.Payments.Where(x => x.InvoiceId == invoiceId && x.PaymentType == "Invoice")
                    .Select(x => x.Amount).ToList();
                    if (payments.Sum() != invoice.PaidAmount)
                    throw new BusinessException("Tổng phiếu thu không khớp hóa đơn, cần đối soát trước khi tiếp tục.");
                    if (amount > invoice.TotalDue - invoice.PaidAmount)
                    throw new BusinessException("Số tiền lớn hơn công nợ còn lại.");
                    // Thanh toán bổ sung không thay đổi hạn gốc của hóa đơn.
                    invoice.PaidAmount += amount;
                    invoice.InvoiceStatus = invoice.PaidAmount == invoice.TotalDue ? "Paid" : "PartiallyPaid";
                    if (invoice.InvoiceStatus == "Paid")
                    {
                        var openReviews = db.InvoiceReviewCases.Where(x => x.InvoiceId == invoiceId && x.ReviewStatus == "Open").ToList();
                        foreach (var review in openReviews)
                        {
                            review.ReviewStatus = "Resolved";
                            review.ResolvedAt = Now();
                        }
                    }
                    db.Payments.Add(new PaymentEntity
                    {
                        PaymentCode = Code("PT"), InvoiceId = invoiceId, 
                        PaymentType = "Invoice", Amount = amount, PaymentMethod = method, PaidAt = Now(), 
                        ReceivedByUserId = actor, Notes = "Thu trễ hạn có phê duyệt: " + approvalReason
                    });
                    Audit(db, actor, "LATE_INVOICE_PAYMENT", "Invoices", invoiceId, 
                    "Thu trễ " + amount.ToString("N0") + "; " + approvalReason);
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Hóa đơn đã thay đổi từ máy khác, hãy tải lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public void PayInvoice(int actor, int invoiceId, decimal amount, string method, string bankReference = null)
        { //Thu hóa đơn, chặn thu vượt và trùng tham chiếu(441-502).
            ValidateMoney(amount);
            if (method != "Cash" && method != "BankTransfer")
            {
                throw new BusinessException("Phương thức thanh toán không hợp lệ.");
            }
            bankReference = (bankReference??"").Trim();
            if (method == "BankTransfer" && (bankReference.Length<5 || bankReference.Length>100))
            throw new BusinessException("Thu qua ngân hàng cần mã tham chiếu giao dịch 5–100 ký tự.");
            if (method == "Cash" && bankReference.Length>0)
            {
                throw new BusinessException("Tiền mặt không dùng mã chuyển khoản.");
            }
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "INVOICES", "Edit");
                    var invoice = db.Invoices.SingleOrDefault(x => x.InvoiceId == invoiceId);
                    if (invoice == null || invoice.InvoiceStatus == "Cancelled" || invoice.InvoiceStatus == "Paid")
                    throw new BusinessException("Hóa đơn không cho phép thu thêm.");
                    if (invoice.InvoiceStatus == "Overdue")
                    throw new BusinessException("Hóa đơn quá hạn; cần xử lý vi phạm trước khi tiếp tục thu.");
                    // Hóa đơn quá hạn nhưng nhân viên chưa bấm 'Ghi nhận quá hạn' vẫn không được thu lách quy trình.
                    // Thu muộn cần quản lý/kế toán giải quyết, không tự sửa ngày hạn hoặc bỏ qua vi phạm.
                    if (invoice.DueAt < Now())
                    throw new BusinessException("Hóa đơn đã quá thời hạn thanh toán. Ngừng thu tự động và chuyển kế toán đối soát.");
                    if (invoice.TotalDue <= invoice.PaidAmount)
                    throw new BusinessException("Hóa đơn không còn số tiền cần thu.");
                    if (amount > invoice.TotalDue - invoice.PaidAmount)
                    {
                        throw new BusinessException("Số thu vượt số tiền còn phải thanh toán.");
                    }
                    if (method == "BankTransfer" && db.Payments.Any(p => p.TransactionReference == bankReference))
                    throw new BusinessException("Mã tham chiếu đã được sử dụng ở chứng từ thanh toán khác.");
                    invoice.PaidAmount += amount;
                    invoice.InvoiceStatus = invoice.PaidAmount == invoice.TotalDue ? "Paid" : "PartiallyPaid";
                    db.Payments.Add(new PaymentEntity
                    {
                        PaymentCode = Code("PT"), InvoiceId = invoiceId, 
                        PaymentType = "Invoice", Amount = amount, PaymentMethod = method, 
                        TransactionReference = method == "BankTransfer"?bankReference:null, PaidAt = Now(), 
                        ReceivedByUserId = actor, Notes = "Thu tiền hóa đơn"
                    });
                    Audit(db, actor, "PAY_INVOICE", "Invoices", invoiceId, "Thu tiền " + amount.ToString("N0"));
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Hóa đơn vừa thay đổi tại máy khác.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
    }
    public sealed class DepositService : ServiceBase
    {
        public DepositService(Func<AuctionArenaDbContext> factory = null) : base(factory) //Khởi tạo DepositService. Nhận phụ thuộc/cấu hình ban đầu
        {
        }
        public void Collect(int actor, int registrationId, decimal amount, string method)//Thu tiền cọc và cập nhật đăng ký(509-560)
        {
            ValidateMoney(amount);
            if (method != "Cash" && method != "BankTransfer")
            {
                throw new BusinessException("Phương thức thu cọc không hợp lệ.");
            }
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "REGISTRATIONS", "Edit");
                    var reg = db.Registrations.Single(x => x.RegistrationId == registrationId);
                    var customer = db.Customers.Single(x => x.CustomerId == reg.CustomerId);
                    if (customer.IsBlacklisted || customer.HasOutstandingDebt)
                    {
                        throw new BusinessException("Khách bị chặn hoặc còn nợ.");
                    }
                    if (reg.RegistrationStatus == "Blocked" || reg.RegistrationStatus == "Closed")
                    throw new BusinessException("Đăng ký đã đóng, không thể thu thêm cọc.");
                    var sessionStatus = db.AuctionSessions.Where(x => x.SessionId == reg.SessionId)
                    .Select(x => x.SessionStatus).Single();
                    if (sessionStatus != "Planned" && sessionStatus != "Open")
                    throw new BusinessException("Chỉ thu tiền cọc trong phiên đang chuẩn bị hoặc đang mở; phiên đã đóng/hủy không được thu thêm.");
                    decimal remaining = reg.DepositRequired-reg.DepositPaid;
                    if (remaining<=0m || amount>remaining)
                    throw new BusinessException("Số thu vượt phần tiền cọc còn thiếu: "+Math.Max(0m, remaining).ToString("N0")+" đ.");
                    reg.DepositPaid += amount;
                    if (reg.DepositPaid >= reg.DepositRequired) reg.RegistrationStatus = "Active";
                    db.Payments.Add(new PaymentEntity
                    {
                        PaymentCode = Code("PT"), RegistrationId = registrationId, 
                        PaymentType = "Deposit", Amount = amount, PaymentMethod = method, PaidAt = Now(), 
                        ReceivedByUserId = actor, Notes = "Thu tiền cọc"
                    });
                    Audit(db, actor, "COLLECT_DEPOSIT", "Registrations", registrationId, "Thu cọc " + amount.ToString("N0"));
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Số dư cọc đã thay đổi.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public void Refund(int actor, int registrationId) //Hoàn cọc còn đủ điều kiện(561-609).
        {
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "REGISTRATIONS", "Edit");
                    var reg = db.Registrations.Single(x => x.RegistrationId == registrationId);
                    if (db.AuctionSessions.Single(x => x.SessionId == reg.SessionId).SessionStatus != "Closed")
                    throw new BusinessException("Phiên chưa kết thúc.");
                    if (db.AuctionLots.Any(x => x.SessionId == reg.SessionId && x.WinningCustomerId == reg.CustomerId && x.LotStatus == "Sold"))
                    throw new BusinessException("Khách thắng lot, chưa thể hoàn cọc tự động.");
                    if (db.AuctionLots.Any(x => x.SessionId == reg.SessionId && (x.LotStatus == "Open" || x.LotStatus == "Planned")))
                    throw new BusinessException("Phiên còn lot chưa chốt.");
                    decimal amount = reg.DepositPaid - reg.DepositApplied - reg.DepositRefunded - reg.DepositForfeited;
                    ValidateMoney(amount);
                    reg.DepositRefunded += amount;
                    reg.RegistrationStatus = "Closed";
                    var obligation = db.DepositRefundObligations.SingleOrDefault(x => x.RegistrationId == registrationId);
                    if (obligation != null)
                    {
                        if (obligation.ObligationStatus != "Pending" || obligation.DueAmount != amount)
                        throw new BusinessException("Nghĩa vụ hoàn cọc không khớp số dư thực tế.");
                        obligation.ObligationStatus = "Settled";
                        obligation.SettledAt = Now();
                    }
                    db.Payments.Add(new PaymentEntity
                    {
                        PaymentCode = Code("PT"), RegistrationId = registrationId, 
                        PaymentType = "Refund", Amount = amount, PaymentMethod = "Cash", PaidAt = Now(), 
                        ReceivedByUserId = actor, Notes = "Hoàn cọc khách không thắng"
                    });
                    Audit(db, actor, "REFUND_DEPOSIT", "Registrations", registrationId, "Hoàn cọc " + amount.ToString("N0"));
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Số dư cọc đã thay đổi.");
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
namespace AuctionArena.Services
{
    public sealed class SettlementService : ServiceBase
    {
        public SettlementService(Func<AuctionArenaDbContext> factory = null) : base(factory) //Khởi tạo SettlementService. Nhận phụ thuộc/cấu hình ban đầu
        {
        }
        // Với lô lịch sử, kế toán xác nhận và khóa tỷ lệ phí trước khi chi trả.
        public void ConfirmLegacySellerFee(int actor, int lotId, decimal rate)// Xác nhận tỷ lệ phí chủ hàng cho dữ liệu chưa chốt phí(620-647)
        {
            if (rate<0m || rate>1m)
            {
                throw new BusinessException("Tỷ lệ phí phải từ 0% đến 100%.");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "PAYOUTS", "Edit");
                    var lot = db.AuctionLots.Single(x => x.LotId == lotId);
                    if (lot.LotStatus != "Sold" || !lot.HammerPrice.HasValue || db.SellerPayouts.Any(x => x.LotId == lotId))
                    throw new BusinessException("Lot chưa bán hoặc đã được chi trả.");
                    if (lot.SellerFeeRateLocked.HasValue)
                    throw new BusinessException("Tỷ lệ phí của lot đã được chốt, không được sửa khi không có chứng từ điều chỉnh.");
                    lot.SellerFeeRateLocked = rate;
                    Audit(db, actor, "CONFIRM_SELLER_FEE", "AuctionLots", lotId, "Chốt phí người bán "+(rate*100m).ToString("0.##")+"%");
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
        // Tách lập lệnh chờ chi và ghi nhận thực chi, với giá trị được đóng băng tại thời điểm lập lệnh.
        public int PrepareSellerPayout(int actor, int lotId) //Lập lệnh chi cho một lô.
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "PAYOUTS", "Create");
                    var lot = db.AuctionLots.SingleOrDefault(x => x.LotId == lotId);
                    if (lot == null || lot.LotStatus != "Sold" || !lot.HammerPrice.HasValue)
                    throw new BusinessException("Lot chưa có kết quả bán hợp lệ.");
                    if (db.SellerPayouts.Any(x => x.LotId == lotId) || db.SellerPayoutOrders.Any(x => x.LotId == lotId))
                    throw new BusinessException("Lot đã có lệnh chi hoặc chứng từ chi trả.");
                    if (!lot.SellerFeeRateLocked.HasValue)
                    throw new BusinessException("Chưa có tỷ lệ phí đã chốt cho lot này.");
                    bool invoicePaid = (from d in db.InvoiceDetails join i in db.Invoices on d.InvoiceId equals i.InvoiceId
                    where d.LotId == lotId && i.InvoiceStatus == "Paid" && 
                    i.CustomerId == lot.WinningCustomerId && i.SessionId == lot.SessionId
                    select d).Any();
                    if (!invoicePaid)
                    {
                        throw new BusinessException("Khách mua chưa thanh toán đủ.");
                    }
                    var consignorId = db.Items.Where(x => x.ItemId == lot.ItemId).Select(x => x.ConsignorId).Single();
                    var rate = lot.SellerFeeRateLocked.Value;
                    var fee = Math.Round(lot.HammerPrice.Value*rate, 2, MidpointRounding.AwayFromZero);
                    var order = new SellerPayoutOrderEntity
                    {
                        LotId = lotId, ConsignorId = consignorId, 
                        HammerPrice = lot.HammerPrice.Value, SellerFeeRate = rate, SellerFee = fee, 
                        PayoutAmount = lot.HammerPrice.Value-fee, OrderStatus = "Pending", 
                        CreatedAt = Now(), CreatedByUserId = actor
                    };
                    db.SellerPayoutOrders.Add(order);
                    db.SaveChanges();
                    Audit(db, actor, "PREPARE_PAYOUT", "SellerPayoutOrders", order.OrderId, 
                    "Lập lệnh chờ chi cho lot "+lotId+"; chưa xuất tiền");
                    db.SaveChanges();
                    tx.Commit();
                    return order.OrderId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Nhiều lệnh chờ chi trong cùng transaction; không chi tiền tại bước này.
        public int PreparePayoutBatch(int actor, System.Collections.Generic.IList<int> lotIds)
        { //Lập lệnh chi nhiều lô trong giao dịch(697-744)
            if (lotIds == null || lotIds.Count == 0 || lotIds.Count>100 || lotIds.Distinct().Count() != lotIds.Count)
            throw new BusinessException("Chọn 1–100 lot duy nhất để lập bảng kê chờ chi.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "PAYOUTS", "Create");
                    var ids = lotIds.ToList();
                    var lots = db.AuctionLots.Where(x => ids.Contains(x.LotId)).ToList();
                    if (lots.Count != ids.Count)
                    {
                        throw new BusinessException("Một số lot đã bị xóa hoặc không tồn tại.");
                    }
                    var items = db.Items.Where(x => lots.Select(l => l.ItemId).Contains(x.ItemId)).ToList();
                    foreach (var lot in lots)
                    {
                        if (lot.LotStatus != "Sold" || !lot.HammerPrice.HasValue || !lot.SellerFeeRateLocked.HasValue)
                        throw new BusinessException("Lot #"+lot.LotNumber+" chưa bán hoặc chưa chốt tỷ lệ phí.");
                        if (db.SellerPayouts.Any(p => p.LotId == lot.LotId) || db.SellerPayoutOrders.Any(p => p.LotId == lot.LotId))
                        throw new BusinessException("Lot #"+lot.LotNumber+" đã có lệnh chi/chứng từ chi.");
                        if (!(from d in db.InvoiceDetails join inv in db.Invoices on d.InvoiceId equals inv.InvoiceId
                        where d.LotId == lot.LotId && inv.InvoiceStatus == "Paid" && 
                        inv.CustomerId == lot.WinningCustomerId && inv.SessionId == lot.SessionId select d).Any())
                        throw new BusinessException("Lot #"+lot.LotNumber+" chưa có hóa đơn thanh toán đủ.");
                        var item = items.Single(i => i.ItemId == lot.ItemId);
                        var fee = Math.Round(lot.HammerPrice.Value*lot.SellerFeeRateLocked.Value, 2, MidpointRounding.AwayFromZero);
                        db.SellerPayoutOrders.Add(new SellerPayoutOrderEntity
                        {
                            LotId = lot.LotId, ConsignorId = item.ConsignorId, HammerPrice = lot.HammerPrice.Value, 
                            SellerFeeRate = lot.SellerFeeRateLocked.Value, SellerFee = fee, 
                            PayoutAmount = lot.HammerPrice.Value-fee, OrderStatus = "Pending", CreatedAt = Now(), CreatedByUserId = actor
                        });
                    }
                    Audit(db, actor, "PREPARE_PAYOUT_BATCH", "SellerPayoutOrders", ids[0], 
                    "Lập "+ids.Count+" lệnh chờ chi; lot: "+string.Join(",", ids));
                    db.SaveChanges();
                    tx.Commit();
                    return lots.Count;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Chỉ hủy lệnh chưa chi: không xóa chứng từ, giữ một mã lệnh theo mỗi lô.
        public void CancelPendingPayoutOrder(int actor, int lotId, string reason) //Hủy lệnh chi chưa thực chi(746-773).
        {
            reason = (reason??"").Trim();
            if (reason.Length<5 || reason.Length>200)
            {
                throw new BusinessException("Cần lý do hủy lệnh 5–200 ký tự.");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "PAYOUTS", "Edit");
                    var order = db.SellerPayoutOrders.SingleOrDefault(x => x.LotId == lotId);
                    if (order == null || order.OrderStatus != "Pending" || db.SellerPayouts.Any(x => x.LotId == lotId))
                    throw new BusinessException("Chỉ được hủy lệnh chưa thực chi.");
                    order.OrderStatus = "Cancelled";
                    Audit(db, actor, "CANCEL_PAYOUT_ORDER", "SellerPayoutOrders", order.OrderId, 
                    "Pending -> Cancelled; "+reason);
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
        // Không tạo đơn trùng khóa LotId: mở lại chính lệnh Cancelled sau khi kiểm tra.
        public void ReopenCancelledPayoutOrder(int actor, int lotId, string approval)//Mở lại lệnh chi đã hủy có xác nhận(715-814).

        {
            approval = (approval??"").Trim();
            if (approval.Length<10 || approval.Length>200)
            {
                throw new BusinessException("Cần căn cứ mở lại từ 10 đến 200 ký tự.");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "PAYOUTS", "Edit");
                    var order = db.SellerPayoutOrders.SingleOrDefault(x => x.LotId == lotId);
                    if (order == null || order.OrderStatus != "Cancelled" || db.SellerPayouts.Any(x => x.LotId == lotId))
                    throw new BusinessException("Lệnh không ở trạng thái Đã hủy hoặc đã thực chi.");
                    var lot = db.AuctionLots.Single(x => x.LotId == lotId);
                    if (lot.LotStatus != "Sold" || !lot.HammerPrice.HasValue || !lot.SellerFeeRateLocked.HasValue)
                    throw new BusinessException("Kết quả lot không còn hợp lệ.");
                    var consignorId = db.Items.Where(x => x.ItemId == lot.ItemId).Select(x => x.ConsignorId).Single();
                    decimal fee = Math.Round(lot.HammerPrice.Value*lot.SellerFeeRateLocked.Value, 2, MidpointRounding.AwayFromZero);
                    if (order.ConsignorId != consignorId || order.HammerPrice != lot.HammerPrice.Value || 
                    order.SellerFeeRate != lot.SellerFeeRateLocked.Value || order.SellerFee != fee || 
                    order.PayoutAmount != lot.HammerPrice.Value-fee || 
                    !(from d in db.InvoiceDetails join i in db.Invoices on d.InvoiceId equals i.InvoiceId
                    where d.LotId == lotId && i.InvoiceStatus == "Paid" && i.CustomerId == lot.WinningCustomerId
                    select d).Any())
                    throw new BusinessException("Chứng từ gốc đã thay đổi. Không thể mở lệnh cũ.");
                    order.OrderStatus = "Pending";
                    Audit(db, actor, "REOPEN_PAYOUT_ORDER", "SellerPayoutOrders", order.OrderId, 
                    "Cancelled -> Pending; "+approval);
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
        public int PaySeller(int actor, int lotId, string method = "Cash", string transferReference = null) //Ghi thực chi và kiểm soát trả trùng(815-895)
        {
            if (method != "Cash" && method != "BankTransfer")
            throw new BusinessException("Phương thức chi trả chủ hàng không hợp lệ.");
            transferReference = (transferReference??"").Trim();
            if (method == "BankTransfer" && (transferReference.Length<5 || transferReference.Length>100))
            throw new BusinessException("Cần mã chuyển khoản thực tế (5–100 ký tự).");
            if (method == "Cash" && transferReference != "")
            {
                throw new BusinessException("Mã ngân hàng không dùng cho tiền mặt.");
            }
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "PAYOUTS", "Create");
                    var lot = db.AuctionLots.SingleOrDefault(x => x.LotId == lotId);
                    if (lot == null || lot.LotStatus != "Sold" || !lot.HammerPrice.HasValue)
                    throw new BusinessException("Lot chưa được bán hợp lệ.");
                    if (db.SellerPayouts.Any(x => x.LotId == lotId))
                    {
                        throw new BusinessException("Lot đã chi trả chủ hàng.");
                    }
                    var order = db.SellerPayoutOrders.SingleOrDefault(x => x.LotId == lotId);
                    if (order == null || order.OrderStatus != "Pending")
                    throw new BusinessException("Chưa có lệnh chi ở trạng thái Chờ chi. Hãy lập lệnh trước.");
                    bool paid = (from d in db.InvoiceDetails join i in db.Invoices on d.InvoiceId equals i.InvoiceId
                    where d.LotId == lotId && i.InvoiceStatus == "Paid" && 
                    i.CustomerId == lot.WinningCustomerId && i.SessionId == lot.SessionId
                    select d).Any();
                    if (!paid)
                    {
                        throw new BusinessException("Hóa đơn của lot chưa thanh toán đủ.");
                    }
                    var item = db.Items.Single(x => x.ItemId == lot.ItemId);
                    // Chuyển khoản chỉ được ghi nhận sau khi hồ sơ có đủ ngân hàng và số tài khoản.
                    // Đây là xác nhận chuyển khoản đã thực hiện, không tự chuyển tiền qua ngân hàng.
                    var consignor = db.Consignors.Single(x => x.ConsignorId == item.ConsignorId);
                    if (method == "BankTransfer" && 
                    (string.IsNullOrWhiteSpace(consignor.BankName) || string.IsNullOrWhiteSpace(consignor.BankAccount)))
                    throw new BusinessException("Chủ hàng chưa khai đủ ngân hàng và số tài khoản. Hãy cập nhật hồ sơ trước khi xác nhận đã chuyển tiền.");
                    if (method == "BankTransfer" && db.SellerPayouts.Any(p => p.TransferReference == transferReference))
                    throw new BusinessException("Mã tham chiếu ngân hàng đã được sử dụng.");
                    if (!lot.SellerFeeRateLocked.HasValue)
                    {
                        throw new BusinessException("Lot cũ chưa chốt tỷ lệ phí bán. Kế toán cần xác nhận tỷ lệ trước khi chi trả.");
                    }
                    decimal rate = lot.SellerFeeRateLocked.Value;
                    decimal fee = Math.Round(lot.HammerPrice.Value * rate, 2, MidpointRounding.AwayFromZero);
                    if (order.ConsignorId != item.ConsignorId || order.HammerPrice != lot.HammerPrice.Value || 
                    order.SellerFeeRate != rate || order.SellerFee != fee || 
                    order.PayoutAmount != lot.HammerPrice.Value-fee)
                    throw new BusinessException("Số tiền của lệnh chờ chi không khớp kết quả lot; ngừng chi để đối soát.");
                    var payout = new AuctionArena.Data.Models.SellerPayoutEntity
                    {
                        PayoutCode = Code("CT"), LotId = lotId, ConsignorId = item.ConsignorId, 
                        HammerPrice = lot.HammerPrice.Value, SellerFeeRate = rate, SellerFee = fee, 
                        PayoutAmount = lot.HammerPrice.Value - fee, PayoutAt = Now(), PaidByUserId = actor, 
                        PaymentMethod = method, TransferReference = method == "BankTransfer"?transferReference:null
                    };
                    db.SellerPayouts.Add(payout);
                    order.OrderStatus = "Paid";
                    order.PaidAt = Now();
                    Audit(db, actor, "SELLER_PAYOUT", "AuctionLots", lotId, "Chi trả chủ hàng theo lệnh #"+order.OrderId);
                    db.SaveChanges();
                    tx.Commit();
                    return payout.PayoutId;
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Lot vừa được thay đổi, vui lòng kiểm tra lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public sealed class DeliveryLine
        {
            public int ItemId
            {
                get;
                set;
            }
            public string Condition
            {
                get;
                set;
            }
        }
        static string IssueDraftPayload(System.Collections.Generic.IList<DeliveryLine> lines) //Mã hóa danh sách hiện vật nháp thành XML
        {
            return new System.Xml.Linq.XElement("Items", lines.OrderBy(x => x.ItemId).Select(x => 
            new System.Xml.Linq.XElement("Item", new System.Xml.Linq.XAttribute("Id", x.ItemId), 
            new System.Xml.Linq.XAttribute("Condition", x.Condition.Trim()))))
            .ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        }
        // Lưu nháp chưa xuất hiện vật và chưa cập nhật trạng thái kho(917-965)
        public int SaveIssueDraft(int actor, int invoiceId, string recipient, 
        System.Collections.Generic.IList<DeliveryLine> lines, string document, string relation, string method)
        {
            recipient = (recipient??"").Trim();
            document = (document??"").Trim();
            relation = (relation??"").Trim();
            if (recipient.Length<2 || recipient.Length>120 || document.Length<6 || document.Length>30 || 
            !System.Text.RegularExpressions.Regex.IsMatch(document, @"^[a-zA-Z0-9]+$") || 
            relation.Length<2 || relation.Length>160 || (method != "AtVenue" && method != "Delivery") || 
            lines == null || lines.Count == 0 || lines.Count>100 || lines.Any(x => x == null) || 
            lines.Select(x => x.ItemId).Distinct().Count() != lines.Count || 
            lines.Any(x => string.IsNullOrWhiteSpace(x.Condition) || x.Condition.Trim().Length>400))
            throw new BusinessException("Phiếu xuất nháp chưa đủ người nhận, chứng từ, hoặc tình trạng từng món.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "STOCK_ISSUES", "Create");
                    if (!db.Invoices.Any(x => x.InvoiceId == invoiceId && x.InvoiceStatus == "Paid"))
                    throw new BusinessException("Hóa đơn chưa thanh toán đầy đủ.");
                    var ids = lines.Select(x => x.ItemId).ToList();
                    var invIds = (from d in db.InvoiceDetails join l in db.AuctionLots on d.LotId equals l.LotId
                    where d.InvoiceId == invoiceId select l.ItemId).ToList();
                    if (ids.Any(x => !invIds.Contains(x)) || 
                    db.Items.Any(x => ids.Contains(x.ItemId) && x.ItemStatus != "Sold") || 
                    db.StockIssueDetails.Any(x => ids.Contains(x.ItemId)))
                    throw new BusinessException("Hiện vật đã xuất hoặc không còn hợp lệ.");
                    var header = new StockIssueEntity
                    {
                        InvoiceId = invoiceId, IssueCode = Code("XN"), 
                        IssuedAt = Now(), IssuedByUserId = actor, RecipientName = recipient, 
                        RecipientDocument = document, RecipientRelation = relation, DeliveryMethod = method, 
                        WorkflowStatus = "Draft", DraftPayload = IssueDraftPayload(lines), 
                        Notes = "Lưu nháp, chưa bàn giao thực tế"
                    };
                    db.StockIssues.Add(header);
                    db.SaveChanges();
                    Audit(db, actor, "SAVE_STOCK_ISSUE_DRAFT", "StockIssues", header.IssueId, "Lưu phiếu xuất nháp, chưa giảm kho");
                    db.SaveChanges();
                    tx.Commit();
                    return header.IssueId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public void CancelIssueDraft(int actor, int draftId, string reason) //Hủy nháp bàn giao(966-990).
        {
            reason = (reason??"").Trim();
            if (reason.Length<5 || reason.Length>200)
            throw new BusinessException("Cần nhập lý do hủy nháp từ 5 đến 200 ký tự.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "STOCK_ISSUES", "Edit");
                    var issue = db.StockIssues.SingleOrDefault(x => x.IssueId == draftId);
                    if (issue == null || issue.WorkflowStatus != "Draft" || db.StockIssueDetails.Any(x => x.IssueId == draftId))
                    throw new BusinessException("Chỉ hủy phiếu nháp chưa giao bất kỳ hiện vật nào.");
                    issue.WorkflowStatus = "Cancelled";
                    Audit(db, actor, "CANCEL_STOCK_ISSUE_DRAFT", "StockIssues", draftId, reason);
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
        public int ConfirmIssueDraft(int actor, int draftId) //Đọc nháp và chuyển sang luồng xác nhận bàn giao(991-1010)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "Create");
                var issue = db.StockIssues.AsNoTracking().SingleOrDefault(x => x.IssueId == draftId && x.WorkflowStatus == "Draft");
                if (issue == null)
                {
                    throw new BusinessException("Phiếu không còn ở trạng thái Nháp.");
                }
                var entries = System.Xml.Linq.XElement.Parse(issue.DraftPayload).Elements("Item");
                var lines = entries.Select(x => new DeliveryLine
                {
                    ItemId = (int)x.Attribute("Id"), 
                    Condition = (string)x.Attribute("Condition")
                }).ToList();
                return IssueItems(actor, issue.InvoiceId, issue.RecipientName, lines, 
                issue.RecipientDocument, issue.RecipientRelation, issue.DeliveryMethod, draftId);
            }
        }
        // Giao từng phần theo thực tế; không tự đánh dấu đã giao những món chưa chọn.
        public int IssueItems(int actor, int invoiceId, string recipient, //Ghi phiếu và chi tiết, chỉ chuyển các hiện vật đã chọn sang Delivered.

        System.Collections.Generic.IList<DeliveryLine> lines, string document, string relation, string method, int? draftIssueId = null)
        {
            if (string.IsNullOrWhiteSpace(recipient) || recipient.Trim().Length>120)
            throw new BusinessException("Cần tên người nhận hợp lệ.");
            document = (document??"").Trim();
            relation = (relation??"").Trim();
            if (document.Length<6 || document.Length>30 || 
            !System.Text.RegularExpressions.Regex.IsMatch(document, @"^[a-zA-Z0-9]+$"))
            throw new BusinessException("CCCD/hộ chiếu 6–30 ký tự chữ và số.");
            if (relation.Length<2 || relation.Length>160)
            {
                throw new BusinessException("Cần ghi quan hệ hoặc số giấy ủy quyền.");
            }
            if (method != "AtVenue" && method != "Delivery")
            {
                throw new BusinessException("Hình thức giao không hợp lệ.");
            }
            if (lines == null || lines.Count == 0 || lines.Count>100 || lines.Any(x => x == null || 
            string.IsNullOrWhiteSpace(x.Condition) || x.Condition.Trim().Length>400) || 
            lines.Select(x => x.ItemId).Distinct().Count() != lines.Count)
            throw new BusinessException("Chọn hiện vật, ghi tình trạng lúc giao cho từng món, không chọn trùng.");
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "STOCK_ISSUES", "Create");
                    var invoice = db.Invoices.Single(x => x.InvoiceId == invoiceId);
                    if (invoice.InvoiceStatus != "Paid")
                    {
                        throw new BusinessException("Hóa đơn chưa thanh toán đủ.");
                    }
                    var itemIds = lines.Select(x => x.ItemId).ToList();
                    var invoiceItems = (from d in db.InvoiceDetails join l in db.AuctionLots on d.LotId equals l.LotId
                    where d.InvoiceId == invoiceId select l.ItemId).ToList();
                    if (itemIds.Any(x => !invoiceItems.Contains(x)))
                    {
                        throw new BusinessException("Hiện vật không thuộc hóa đơn.");
                    }
                    var lotIds = db.InvoiceDetails.Where(x => x.InvoiceId == invoiceId).Select(x => x.LotId).ToList();
                    if (db.AuctionLots.Any(x => lotIds.Contains(x.LotId) && 
                    (x.LotStatus != "Sold" || x.WinningCustomerId != invoice.CustomerId)))
                    throw new BusinessException("Một lot trong hóa đơn đã đổi trạng thái hoặc người thắng; ngừng giao hàng để đối soát.");
                    if (db.StockIssueDetails.Any(x => itemIds.Contains(x.ItemId)))
                    throw new BusinessException("Một hiện vật đã được xuất ở phiếu trước; hãy tải lại.");
                    var items = db.Items.Where(x => itemIds.Contains(x.ItemId)).ToList();
                    if (items.Count != itemIds.Count || items.Any(x => x.ItemStatus != "Sold"))
                    throw new BusinessException("Có hiện vật không còn hợp lệ để xuất.");
                    StockIssueEntity issue;
                    if (draftIssueId.HasValue)
                    {
                        issue = db.StockIssues.SingleOrDefault(x => x.IssueId == draftIssueId.Value);
                        if (issue == null || issue.WorkflowStatus != "Draft" || issue.InvoiceId != invoiceId || 
                        issue.RecipientName != recipient.Trim() || issue.RecipientDocument != document || 
                        issue.RecipientRelation != relation || issue.DeliveryMethod != method || 
                        issue.DraftPayload != IssueDraftPayload(lines) || 
                        db.StockIssueDetails.Any(x => x.IssueId == issue.IssueId))
                        throw new BusinessException("Phiếu nháp đã bị thay đổi. Hãy tải lại để xác nhận.");
                        issue.WorkflowStatus = "Confirmed";
                        issue.IssuedAt = Now();
                        issue.DraftPayload = null;
                        issue.Notes = "Xác nhận bàn giao từ phiếu nháp";
                    }
                    else
                    {
                        issue = new StockIssueEntity
                        {
                            IssueCode = Code("XK"), InvoiceId = invoiceId, 
                            IssuedAt = Now(), IssuedByUserId = actor, RecipientName = recipient.Trim(), 
                            RecipientDocument = document, RecipientRelation = relation, DeliveryMethod = method, 
                            WorkflowStatus = "Confirmed", Notes = "Giao từng phần sau thanh toán đủ"
                        };
                        db.StockIssues.Add(issue);
                    }
                    db.SaveChanges();
                    foreach (var item in items)
                    {
                        db.StockIssueDetails.Add(new AuctionArena.Data.Models.StockIssueDetailEntity
                        {
                            IssueId = issue.IssueId, ItemId = item.ItemId, 
                            ConditionAtDelivery = lines.Single(x => x.ItemId == item.ItemId).Condition.Trim(), 
                            LocationAtIssue = item.WarehouseLocation
                        });
                        item.ItemStatus = "Delivered";
                    }
                    Audit(db, actor, "STOCK_ISSUE", "StockIssues", issue.IssueId, "Giao " + items.Count + " hiện vật");
                    db.SaveChanges();
                    tx.Commit();
                    return issue.IssueId;
                }
                catch (DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Kho vừa thay đổi; hãy kiểm tra tồn kho.");
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
