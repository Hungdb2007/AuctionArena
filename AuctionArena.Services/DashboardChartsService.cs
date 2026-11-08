using System;
using System.Collections.Generic;
using System.Linq;
using AuctionArena.Data;
namespace AuctionArena.Services
{
    public sealed class MonthRevenue
    {
        public int Year
        {
            get;
            set;
        }
        public int Month
        {
            get;
            set;
        }
        public decimal Revenue
        {
            get;
            set;
        }
    }
    public sealed class CategoryShare
    {
        public string Category
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
    public sealed class TopHammerLot
    {
        public string Item
        {
            get;
            set;
        }
        public decimal Price
        {
            get;
            set;
        }
    }
    public sealed class DashboardChartsData
    {
        public List<MonthRevenue> Months
        {
            get;
            set;
        }
        public List<CategoryShare> Categories
        {
            get;
            set;
        }
        public List<TopHammerLot> TopLots
        {
            get;
            set;
        }
    }
    public sealed class DashboardChartsService : ServiceBase
    {
        public DashboardChartsService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public DashboardChartsData Load(int actor) //Tổng hợp dữ liệu theo điều kiện đầu vào(74-129)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REPORTS", "View");
                // Chỉ xét hóa đơn đã thanh toán. Doanh thu sàn ở đây chưa gồm phí người bán.
                // Chỉ đếm một lần trên ngày thu cuối của hóa đơn đã thanh toán đủ.
                var paid = (from i in db.Invoices where i.InvoiceStatus == "Paid"
                select new
                {
                    Fee = i.BuyerFee-i.VipDiscount, 
                    LastPaid = db.Payments.Where(p => p.InvoiceId == i.InvoiceId && p.PaymentType == "Invoice")
                    .Max(p => (DateTime?)p.PaidAt)
                }).ToList();
                var grouped = paid.Where(x => x.LastPaid.HasValue).GroupBy(x => new
                {
                    x.LastPaid.Value.Year, x.LastPaid.Value.Month
                })
                .ToDictionary(x => x.Key.Year*100+x.Key.Month, x => x.Sum(y => y.Fee));
                var now = DateTime.Today;
                var months = Enumerable.Range(0, 12).Select(i => 
                {
                    var date = new DateTime(now.Year, now.Month, 1).AddMonths(i-11);
                    decimal v; grouped.TryGetValue(date.Year*100+date.Month, out v);
                    return new MonthRevenue
                    {
                        Year = date.Year, Month = date.Month, Revenue = v
                    };
                }).ToList();
                var shares = (from d in db.InvoiceDetails join i in db.Invoices on d.InvoiceId equals i.InvoiceId
                join l in db.AuctionLots on d.LotId equals l.LotId
                join item in db.Items on l.ItemId equals item.ItemId
                join c in db.Categories on item.CategoryId equals c.CategoryId
                where i.InvoiceStatus == "Paid"
                group d by c.CategoryName into g
                select new CategoryShare
                {
                    Category = g.Key, Amount = g.Sum(x => x.HammerPrice)
                })
                .OrderByDescending(x => x.Amount).ToList();
                var top = (from d in db.InvoiceDetails join i in db.Invoices on d.InvoiceId equals i.InvoiceId
                join l in db.AuctionLots on d.LotId equals l.LotId
                join item in db.Items on l.ItemId equals item.ItemId
                where i.InvoiceStatus == "Paid"
                orderby d.HammerPrice descending
                select new TopHammerLot
                {
                    Item = item.ItemName, Price = d.HammerPrice
                })
                .Take(5).ToList();
                return new DashboardChartsData
                {
                    Months = months, Categories = shares, TopLots = top
                };
            }
        }
    }
    public sealed class RecentActivityRow
    {
        public DateTime At
        {
            get;
            set;
        }
        public string Action
        {
            get;
            set;
        }
        public string Description
        {
            get;
            set;
        }
    }
    public sealed class DashboardActivityService : ServiceBase
    {
        public DashboardActivityService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {//Khởi tạo DashboardActivityService. Nhận phụ thuộc/cấu hình ban đầu
        }
        public List<RecentActivityRow> MyLatest(int actor) //Lấy hoạt động gần nhất của người dùng(154-172)
        {
            using (var db = Factory())
            {
                if (!db.Users.Any(u => u.UserId == actor && u.IsActive))
                throw new BusinessException("Phiên đăng nhập không hợp lệ.");
                // Chỉ hiển thị thao tác của chính người dùng đăng nhập, tránh lộ nhật ký người khác.
                return db.AuditLogs.Where(log => log.UserId == actor)
                .OrderByDescending(log => log.OccurredAt)
                .ThenByDescending(log => log.AuditLogId)
                .Take(10)
                .Select(log => new RecentActivityRow
                {
                    At = log.OccurredAt, 
                    Action = log.ActionCode, 
                    Description = log.Description
                }).ToList();
            }
        }
    }
}
