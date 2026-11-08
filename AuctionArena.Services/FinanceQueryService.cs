using System;
using System.Collections.Generic;
using System.Linq;
using AuctionArena.Data;
namespace AuctionArena.Services
{
    // Các DTO chỉ dùng để hiển thị: Form không được truy vấn DbContext.
    public sealed class FinanceInvoiceRow
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
        public string Customer
        {
            get;
            set;
        }
        public string Session
        {
            get;
            set;
        }
        public DateTime IssuedAt
        {
            get;
            set;
        }
        public DateTime DueAt
        {
            get;
            set;
        }
        public string Status
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
        public decimal Remaining
        {
            get
            {
                return TotalDue - PaidAmount;
            }
        }
    }
    public sealed class FinanceLotRow
    {
        public int LotId
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
        public decimal? SellerFeeRate
        {
            get;
            set;
        }
        public decimal? SellerFee
        {
            get
            {
                return SellerFeeRate.HasValue?Math.Round(HammerPrice*SellerFeeRate.Value, 2, MidpointRounding.AwayFromZero):(decimal?)null;
            }
        }
        public decimal? PayoutAmount
        {
            get
            {
                return SellerFee.HasValue?HammerPrice-SellerFee.Value:(decimal?)null;
            }
        }
        public string Consignor
        {
            get;
            set;
        }
        // Chỉ dùng tại màn chi trả, không tự sinh cột thông tin ngân hàng trong DataGridView.
        [System.ComponentModel.Browsable(false)] public string BankName
        {
            get;
            set;
        }
        [System.ComponentModel.Browsable(false)] public string BankAccount
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
        public string PayoutOrderStatus
        {
            get;
            set;
        }
    }
    public sealed class FinancePaymentRow
    {
        public int PaymentId
        {
            get;
            set;
        }
        public string PaymentCode
        {
            get;
            set;
        }
        public string PaymentType
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
        public DateTime PaidAt
        {
            get;
            set;
        }
    }
    public sealed class IssueItemRow
    {
        public int ItemId
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
        public string WarehouseLocation
        {
            get;
            set;
        }
    }
    public sealed class IssueDraftRow
    {
        public int IssueId
        {
            get;
            set;
        }
        public int InvoiceId
        {
            get;
            set;
        }
        public string IssueCode
        {
            get;
            set;
        }
        public string Recipient
        {
            get;
            set;
        }
        public DateTime CreatedAt
        {
            get;
            set;
        }
    }
    public sealed class FinanceIssueRow
    {
        public int IssueId
        {
            get;
            set;
        }
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
        public string Recipient
        {
            get;
            set;
        }
        public string RecipientDocument
        {
            get;
            set;
        }
        public string DeliveryMethod
        {
            get;
            set;
        }
        public DateTime IssuedAt
        {
            get;
            set;
        }
    }
    public sealed class FinancePayoutRow
    {
        public int PayoutId
        {
            get;
            set;
        }
        public int LotId
        {
            get;
            set;
        }
        public string Consignor
        {
            get;
            set;
        }
        public decimal PayoutAmount
        {
            get;
            set;
        }
        public DateTime PaidAt
        {
            get;
            set;
        }
        public string PaymentMethod
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
    }
    public sealed class InvoiceSessionOption
    {
        public int Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
    }
    public sealed class InvoiceCustomerOption
    {
        public int Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
    }
    public sealed class FinanceQueryService : ServiceBase
    {
        public FinanceQueryService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public List<InvoiceSessionOption> InvoiceSessions(int actor)//Lấy phiên có dữ liệu hóa đơn.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                return db.AuctionSessions.Where(s => s.SessionStatus == "Closed")
                .OrderByDescending(s => s.StartsAt)
                .Select(s => new InvoiceSessionOption
                {
                    Id = s.SessionId, Name = s.SessionCode+" – "+s.SessionName
                }).ToList();
            }
        }
        public List<InvoiceCustomerOption> InvoiceCustomers(int actor, int sessionId)//Lấy khách theo phiên để lập/xem hóa đơn.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                return (from l in db.AuctionLots
                join c in db.Customers on l.WinningCustomerId equals (int?)c.CustomerId
                where l.SessionId == sessionId && l.LotStatus == "Sold" && 
                !db.Invoices.Any(i => i.SessionId == sessionId && i.CustomerId == c.CustomerId)
                orderby c.FullName
                select new InvoiceCustomerOption
                {
                    Id = c.CustomerId, Name = c.CustomerCode+" – "+c.FullName
                })
                .Distinct().ToList();
            }
        }
        public List<FinanceInvoiceRow> Invoices(int actor, string keyword, bool onlyDebt, string status = null, DateTime? from = null, DateTime? to = null)
        { //Truy vấn hóa đơn theo bộ lọc(352-382).
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                var query = from i in db.Invoices join c in db.Customers on i.CustomerId equals c.CustomerId
                join s in db.AuctionSessions on i.SessionId equals s.SessionId
                select new FinanceInvoiceRow
                {
                    InvoiceId = i.InvoiceId, InvoiceCode = i.InvoiceCode, 
                    Customer = c.FullName, Session = s.SessionName, IssuedAt = i.IssuedAt, DueAt = i.DueAt, 
                    Status = i.InvoiceStatus, TotalDue = i.TotalDue, PaidAmount = i.PaidAmount
                };
                if (from.HasValue && to.HasValue && from.Value.Date>to.Value.Date)
                throw new BusinessException("Từ ngày phải trước hoặc bằng đến ngày.");
                if (!string.IsNullOrWhiteSpace(status))query = query.Where(x => x.Status == status);
                if (from.HasValue)
                {
                    var start = from.Value.Date;
                    query = query.Where(x => x.IssuedAt>=start);
                }
                if (to.HasValue)
                {
                    var end = to.Value.Date.AddDays(1);
                    query = query.Where(x => x.IssuedAt<end);
                }
                if (onlyDebt) query = query.Where(x => x.TotalDue > x.PaidAmount && x.Status != "Cancelled");
                if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.InvoiceCode.Contains(keyword) || x.Customer.Contains(keyword));
                return query.OrderByDescending(x => x.IssuedAt).ToList();
            }
        }
        public List<FinanceLotRow> InvoiceLots(int actor, int invoiceId) //Lấy các lô của hóa đơn
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                return (from d in db.InvoiceDetails join l in db.AuctionLots on d.LotId equals l.LotId
                join it in db.Items on l.ItemId equals it.ItemId
                join c in db.Consignors on it.ConsignorId equals c.ConsignorId
                where d.InvoiceId == invoiceId
                select new FinanceLotRow
                {
                    LotId = d.LotId, ItemName = it.ItemName, HammerPrice = d.HammerPrice, 
                    Consignor = c.FullName, SellerFeeRate = l.SellerFeeRateLocked
                })
                .OrderBy(x => x.LotId).ToList();
            }
        }
        public List<FinancePaymentRow> InvoicePayments(int actor, int invoiceId) //Lấy khoản thu/chuyển cọc của hóa đơn
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "INVOICES", "View");
                return db.Payments.Where(x => x.InvoiceId == invoiceId)
                .OrderByDescending(x => x.PaidAt).Select(x => new FinancePaymentRow
                {
                    PaymentId = x.PaymentId, PaymentCode = x.PaymentCode, PaymentType = x.PaymentType, Amount = x.Amount, 
                    PaymentMethod = x.PaymentMethod, TransactionReference = x.TransactionReference, PaidAt = x.PaidAt
                }).ToList();
            }
        }
        public List<IssueItemRow> PendingIssueItems(int actor, int invoiceId) //Lấy hiện vật hóa đơn chưa giao
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                if (!db.Invoices.Any(x => x.InvoiceId == invoiceId && x.InvoiceStatus == "Paid"))
                throw new BusinessException("Hóa đơn chưa thanh toán đầy đủ.");
                return (from d in db.InvoiceDetails
                join l in db.AuctionLots on d.LotId equals l.LotId
                join i in db.Items on l.ItemId equals i.ItemId
                where d.InvoiceId == invoiceId && i.ItemStatus == "Sold" && 
                !db.StockIssueDetails.Any(x => x.ItemId == i.ItemId)
                orderby i.ItemCode
                select new IssueItemRow
                {
                    ItemId = i.ItemId, ItemCode = i.ItemCode, 
                    ItemName = i.ItemName, WarehouseLocation = i.WarehouseLocation
                }).ToList();
            }
        }
        public List<IssueDraftRow> IssueDrafts(int actor) //Truy vấn phiếu giao nháp
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                return db.StockIssues.Where(x => x.WorkflowStatus == "Draft")
                .OrderByDescending(x => x.IssuedAt).Select(x => new IssueDraftRow
                {
                    IssueId = x.IssueId, InvoiceId = x.InvoiceId, IssueCode = x.IssueCode, 
                    Recipient = x.RecipientName, CreatedAt = x.IssuedAt
                }).ToList();
            }
        }
        public List<FinanceIssueRow> Issues(int actor)//Truy vấn phiếu xuất.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                return (from x in db.StockIssues join i in db.Invoices on x.InvoiceId equals i.InvoiceId
                where x.WorkflowStatus == null || x.WorkflowStatus == "Confirmed"
                orderby x.IssuedAt descending
                select new FinanceIssueRow
                {
                    IssueId = x.IssueId, IssueCode = x.IssueCode, 
                    InvoiceCode = i.InvoiceCode, Recipient = x.RecipientName, RecipientDocument = x.RecipientDocument, DeliveryMethod = x.DeliveryMethod, IssuedAt = x.IssuedAt
                }).ToList();
            }
        }
        public List<FinancePayoutRow> Payouts(int actor) //Truy vấn chi trả.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "PAYOUTS", "View");
                return (from p in db.SellerPayouts join c in db.Consignors on p.ConsignorId equals c.ConsignorId
                orderby p.PayoutAt descending
                select new FinancePayoutRow
                {
                    PayoutId = p.PayoutId, LotId = p.LotId, 
                    Consignor = c.FullName, PayoutAmount = p.PayoutAmount, PaidAt = p.PayoutAt, 
                    PaymentMethod = p.PaymentMethod, Status = "Đã chi"
                }).ToList();
            }
        }
        public List<FinanceLotRow> EligiblePayouts(int actor) //Lấy lô đủ điều kiện lập chi trả(476-507)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "PAYOUTS", "View");
                var rows = (from d in db.InvoiceDetails join inv in db.Invoices on d.InvoiceId equals inv.InvoiceId
                join l in db.AuctionLots on d.LotId equals l.LotId
                join it in db.Items on l.ItemId equals it.ItemId
                join c in db.Consignors on it.ConsignorId equals c.ConsignorId
                where inv.InvoiceStatus == "Paid" && !db.SellerPayouts.Any(p => p.LotId == l.LotId)
                select new FinanceLotRow
                {
                    LotId = l.LotId, ItemName = it.ItemName, 
                    HammerPrice = d.HammerPrice, Consignor = c.FullName, 
                    BankName = c.BankName, BankAccount = c.BankAccount, 
                    SellerFeeRate = l.SellerFeeRateLocked, Status = "Chờ chi"
                }).OrderBy(x => x.LotId).ToList();
                var lotIds = rows.Select(x => x.LotId).ToList();
                var orders = db.SellerPayoutOrders.Where(x => lotIds.Contains(x.LotId))
                .Select(x => new
                {
                    x.LotId, x.OrderStatus
                }).ToList();
                foreach (var row in rows)
                {
                    var order = orders.FirstOrDefault(x => x.LotId == row.LotId);
                    row.PayoutOrderStatus = order == null?"Chưa lập lệnh":order.OrderStatus == "Pending"?"Chờ chi":
                    order.OrderStatus == "Cancelled"?"Đã hủy":"Đã chi";
                }
                return rows;
            }
        }
        public List<FinanceInvoiceRow> ReadyToIssue(int actor) //Lấy hóa đơn/hiện vật đủ điều kiện giao(508-530).
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_ISSUES", "View");
                return (from i in db.Invoices join c in db.Customers on i.CustomerId equals c.CustomerId
                join s in db.AuctionSessions on i.SessionId equals s.SessionId
                where i.InvoiceStatus == "Paid" && 
                (from d in db.InvoiceDetails
                join l in db.AuctionLots on d.LotId equals l.LotId
                join it in db.Items on l.ItemId equals it.ItemId
                where d.InvoiceId == i.InvoiceId && it.ItemStatus == "Sold" && 
                !db.StockIssueDetails.Any(x => x.ItemId == it.ItemId)
                select it.ItemId).Any()
                orderby i.IssuedAt descending
                select new FinanceInvoiceRow
                {
                    InvoiceId = i.InvoiceId, InvoiceCode = i.InvoiceCode, 
                    Customer = c.FullName, Session = s.SessionName, IssuedAt = i.IssuedAt, DueAt = i.DueAt, 
                    Status = i.InvoiceStatus, TotalDue = i.TotalDue, PaidAmount = i.PaidAmount
                }).ToList();
            }
        }
    }
}
