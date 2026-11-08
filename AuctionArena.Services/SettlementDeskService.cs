using System;
using System.Collections.Generic;
using System.Linq;
using AuctionArena.Data;
namespace AuctionArena.Services
{
    // DTO cho quầy đối soát trực tiếp sau phiên. Các hàm trong lớp này chỉ ĐỌC dữ liệu.
    public sealed class SettlementSessionOption
    {
        public int SessionId
        {
            get;
            set;
        }
        public string Display
        {
            get;
            set;
        }
    }
    public sealed class SettlementWinnerRow
    {
        public int SessionId
        {
            get;
            set;
        }
        public int CustomerId
        {
            get;
            set;
        }
        public string Session
        {
            get;
            set;
        }
        public string Customer
        {
            get;
            set;
        }
        public string CustomerCode
        {
            get;
            set;
        }
        public int? Paddle
        {
            get;
            set;
        }
        public int LotsWon
        {
            get;
            set;
        }
        public decimal HammerTotal
        {
            get;
            set;
        }
        public int? InvoiceId
        {
            get;
            set;
        }
        public string InvoiceCode
        {
            get;
            set;
        }
        public string PaymentStatus
        {
            get;
            set;
        }
        public decimal Remaining
        {
            get;
            set;
        }
        public DateTime? DueAt
        {
            get;
            set;
        }
    }
    public sealed class SettlementLotRow
    {
        public int LotId
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
        public decimal HammerPrice
        {
            get;
            set;
        }
        public string State
        {
            get;
            set;
        }
    }
    public sealed class SettlementCandidateRow
    {
        public string Paddle
        {
            get;
            set;
        }
        public string Customer
        {
            get;
            set;
        }
        public decimal BidAmount
        {
            get;
            set;
        }
        public DateTime BidAt
        {
            get;
            set;
        }
        public string Review
        {
            get;
            set;
        }
    }
    public sealed class SettlementDeskService : ServiceBase
    {
        public SettlementDeskService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public List<SettlementSessionOption> ClosedSessions(int actor) //Lấy phiên đã kết thúc.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                return db.AuctionSessions.Where(s => s.SessionStatus == "Closed")
                .OrderByDescending(s => s.EndsAt)
                .Select(s => new SettlementSessionOption
                {
                    SessionId = s.SessionId, 
                    Display = s.SessionCode + " - " + s.SessionName
                }).ToList();
            }
        }
        public List<SettlementWinnerRow> Winners(int actor, int? sessionId) //Lấy người trúng và số liệu đối soát(164-242)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                // Chỉ thêm điều kiện phiên khi người dùng chọn phiên, tránh đọc Nullable.Value rỗng.
                var lotQuery = db.AuctionLots.Where(x => x.LotStatus == "Sold" && x.HammerPrice.HasValue);
                if (sessionId.HasValue) lotQuery = lotQuery.Where(x => x.SessionId == sessionId.Value);
                // Dùng JOIN và GROUP sau khi lọc lô đã bán của phiên kết thúc.
                var sales = (from lot in lotQuery
                join s in db.AuctionSessions on lot.SessionId equals s.SessionId
                join customer in db.Customers on lot.WinningCustomerId equals (int?)customer.CustomerId
                where s.SessionStatus == "Closed"
                select new
                {
                    lot.SessionId, s.SessionCode, lot.LotId, 
                    customer.CustomerId, customer.CustomerCode, customer.FullName, 
                    Hammer = lot.HammerPrice.Value
                }).ToList();
                if (sales.Count == 0)
                {
                    return new List<SettlementWinnerRow>();
                }
                var selectedSessionIds = sales.Select(x => x.SessionId).Distinct().ToList();
                var selectedCustomerIds = sales.Select(x => x.CustomerId).Distinct().ToList();
                var registrations = db.Registrations
                .Where(x => selectedSessionIds.Contains(x.SessionId) && selectedCustomerIds.Contains(x.CustomerId))
                .Select(x => new
                {
                    x.SessionId, x.CustomerId, x.PaddleNumber
                }).ToList();
                var invoices = db.Invoices
                .Where(x => selectedSessionIds.Contains(x.SessionId) && selectedCustomerIds.Contains(x.CustomerId))
                .Select(x => new
                {
                    x.InvoiceId, x.SessionId, x.CustomerId, x.InvoiceCode, 
                    x.InvoiceStatus, x.TotalDue, x.PaidAmount, x.DueAt
                }).ToList();
                var now = Now();
                return sales.GroupBy(x => new
                {
                    x.SessionId, x.SessionCode, x.CustomerId, 
                    x.CustomerCode, x.FullName
                })
                .Select(g => 
                {
                    var invoice = invoices.FirstOrDefault(i => i.SessionId == g.Key.SessionId && 
                    i.CustomerId == g.Key.CustomerId);
                    var reg = registrations.FirstOrDefault(r => r.SessionId == g.Key.SessionId && 
                    r.CustomerId == g.Key.CustomerId);
                    var status = "Chưa lập hóa đơn";
                    if (invoice != null)
                    {
                        status = invoice.InvoiceStatus == "Paid" ? "Đã thanh toán" :
                        invoice.InvoiceStatus == "Overdue" ? "Quá hạn" :
                        invoice.InvoiceStatus == "Cancelled" ? "Đã hủy hóa đơn" :
                        invoice.DueAt < now && invoice.TotalDue > invoice.PaidAmount ? "Đến hạn chưa trả" :
                        invoice.InvoiceStatus == "PartiallyPaid" ? "Thanh toán một phần" : "Chưa thanh toán";
                    }
                    return new SettlementWinnerRow
                    {
                        SessionId = g.Key.SessionId, 
                        Session = g.Key.SessionCode, 
                        CustomerId = g.Key.CustomerId, 
                        CustomerCode = g.Key.CustomerCode, 
                        Customer = g.Key.FullName, 
                        Paddle = reg == null ? (int?)null : reg.PaddleNumber, 
                        LotsWon = g.Count(), 
                        HammerTotal = g.Sum(x => x.Hammer), 
                        InvoiceId = invoice == null ? (int?)null : invoice.InvoiceId, 
                        InvoiceCode = invoice == null ? "" : invoice.InvoiceCode, 
                        PaymentStatus = status, 
                        DueAt = invoice == null ? (DateTime?)null : invoice.DueAt, 
                        Remaining = invoice == null ? 0 : Math.Max(0m, invoice.TotalDue - invoice.PaidAmount)
                    };
                })
                .OrderBy(x => x.Session).ThenBy(x => x.Paddle).ToList();
            }
        }
        public List<SettlementLotRow> WonLots(int actor, int sessionId, int customerId) //Lấy các lô khách trúng(243-260
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                return (from lot in db.AuctionLots
                join item in db.Items on lot.ItemId equals item.ItemId
                where lot.SessionId == sessionId && lot.WinningCustomerId == customerId && 
                lot.LotStatus == "Sold" && lot.HammerPrice.HasValue
                orderby lot.LotNumber
                select new SettlementLotRow
                {
                    LotId = lot.LotId, LotNumber = lot.LotNumber, 
                    ItemName = item.ItemName, HammerPrice = lot.HammerPrice.Value, 
                    State = lot.LotStatus
                }).ToList();
            }
        }
        // Chỉ phục vụ đối chiếu khi hóa đơn quá hạn. không chuyển quyền mua hay sửa tiền.
        public List<SettlementCandidateRow> ReviewNextBidders(int actor, int lotId) //Tổng hợp người trả giá tiếp theo để đối soát(260-313).
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                SecurityService.Require(db, actor, "BIDS", "View");
                var lot = db.AuctionLots.SingleOrDefault(x => x.LotId == lotId);
                if (lot == null || lot.LotStatus != "Sold" || !lot.WinningCustomerId.HasValue)
                throw new BusinessException("Chỉ tra ứng viên cho lot đã bán và đã chốt người thắng.");
                var invoice = (from d in db.InvoiceDetails
                join i in db.Invoices on d.InvoiceId equals i.InvoiceId
                where d.LotId == lotId && i.CustomerId == lot.WinningCustomerId.Value
                select i).FirstOrDefault();
                if (invoice == null)
                throw new BusinessException("Lot chưa có hóa đơn. Hãy lập hóa đơn trước khi đối soát.");
                if (!(invoice.InvoiceStatus == "Overdue" || 
                ((invoice.InvoiceStatus == "Unpaid" || invoice.InvoiceStatus == "PartiallyPaid") && 
                invoice.DueAt < Now() && invoice.PaidAmount < invoice.TotalDue)))
                throw new BusinessException("Chỉ xem ứng viên khi hóa đơn của người thắng đã quá hạn.");
                // Không truy xuất danh tính cá nhân lên màn chiếu công cộng.
                var bids = db.Bids.Where(x => x.LotId == lotId && x.CustomerId != lot.WinningCustomerId.Value)
                .OrderByDescending(x => x.Amount).ThenBy(x => x.BidAt).ToList();
                var customerIds = bids.Select(b => b.CustomerId).Distinct().ToList();
                var customers = db.Customers.Where(c => customerIds.Contains(c.CustomerId)).ToList();
                var regs = db.Registrations.Where(r => r.SessionId == lot.SessionId && customerIds.Contains(r.CustomerId)).ToList();
                var item = db.Items.Single(x => x.ItemId == lot.ItemId);
                var sellerIdentity = db.Consignors.Where(x => x.ConsignorId == item.ConsignorId)
                .Select(x => x.IdentityNumber).FirstOrDefault();
                return bids.GroupBy(x => x.CustomerId)
                .Select(g => g.OrderByDescending(x => x.Amount).ThenBy(x => x.BidAt).First())
                .OrderByDescending(x => x.Amount).ThenBy(x => x.BidAt)
                .Select(b => 
                {
                    var c = customers.First(x => x.CustomerId == b.CustomerId);
                    var r = regs.FirstOrDefault(x => x.CustomerId == b.CustomerId);
                    string warning = "Cần thẩm tra lại trước khi xử lý";
                    if (b.Amount < lot.ReservePrice) warning = "Dưới giá sàn bảo lưu";
                    else if (c.IsBlacklisted || c.HasOutstandingDebt) warning = "Khách đang bị hạn chế tham gia";
                    else if (!string.IsNullOrWhiteSpace(sellerIdentity) && 
                    !string.IsNullOrWhiteSpace(c.IdentityNumber) && sellerIdentity == c.IdentityNumber)
                    warning = "Có giấy tờ trùng chủ hàng";
                    else if (r == null || r.DepositPaid < r.DepositRequired) warning = "Không đủ cọc tại phiên";
                    else if (r.DepositPaid - r.DepositApplied - r.DepositRefunded - r.DepositForfeited <= 0m)
                    warning = "Cọc không còn khả dụng, cần đối soát";
                    return new SettlementCandidateRow
                    {
                        Customer = c.FullName, Paddle = r == null ? "-" : r.PaddleNumber.ToString(), 
                        BidAmount = b.Amount, BidAt = b.BidAt, Review = warning
                    };
                }).ToList();
            }
        }
    }
}
