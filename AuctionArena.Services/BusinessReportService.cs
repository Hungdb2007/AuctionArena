using System;
using System.Collections.Generic;
using System.Linq;
using AuctionArena.Data;
namespace AuctionArena.Services
{
    // Doanh thu tính theo ngày hóa đơn được thanh toán đủ.
    // Kết quả bán lô tính theo ngày chốt lô, tách khỏi ngày thu tiền.
    public sealed class BusinessReportFilter
    {
        public DateTime From
        {
            get;
            set;
        }
        public DateTime To
        {
            get;
            set;
        }
        public int? SessionId
        {
            get;
            set;
        }
        public int? CategoryId
        {
            get;
            set;
        }
        public int? ConsignorId
        {
            get;
            set;
        }
        public string MemberTier
        {
            get;
            set;
        }
        public string GroupBy
        {
            get;
            set;
        }
    }
    public sealed class BusinessReportOption
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
    public sealed class BusinessReportRow
    {
        public string GroupLabel
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
        public decimal Discount
        {
            get;
            set;
        }
        public decimal Vat
        {
            get;
            set;
        }
        public decimal PlatformRevenue
        {
            get;
            set;
        }
        public decimal UnpaidAmount
        {
            get;
            set;
        }
        public decimal PaidToSeller
        {
            get;
            set;
        }
        public int SoldLots
        {
            get;
            set;
        }
        public int UnsoldLots
        {
            get;
            set;
        }
        public decimal SuccessRate
        {
            get
            {
                var total = SoldLots+UnsoldLots;
                return total == 0?0m:decimal.Round(100m*SoldLots/total, 2);
            }
        }
        public int InvoiceCount
        {
            get;
            set;
        }
        // Phần này chỉ tạo dòng dữ liệu in, không ghi chứng từ.
        public string Outcome
        {
            get
            {
                return SoldLots+" / "+UnsoldLots+" ("+SuccessRate.ToString("0.##")+"%)";
            }
        }
    }
    public sealed class LogoutStatusService : ServiceBase
    {
        public LogoutStatusService(Func<AuctionArenaDbContext> factory = null):base(factory) //Khởi tạo LogoutStatusService. Nhận phụ thuộc/cấu hình ban đầu.
        {
        }
        public int OpenLots(int actor) //Lấy lô còn mở để kiểm tra đăng xuất
        {
            using (var db = Factory())
            {
                var user = db.Users.SingleOrDefault(x => x.UserId == actor && x.IsActive);
                if (user == null)
                {
                    return 0;
                }
                return (from lot in db.AuctionLots
                join session in db.AuctionSessions on lot.SessionId equals session.SessionId
                where session.SessionStatus == "Open" && lot.LotStatus == "Open"
                select lot.LotId).Count();
            }
        }
    }
    public sealed class BusinessReportService : ServiceBase //
    {
        public BusinessReportService(Func<AuctionArenaDbContext> factory = null):base(factory) //Khởi tạo BusinessReportService. Nhận phụ thuộc/cấu hình ban đầu.
        {
        }
        private static void Check(BusinessReportFilter f) //Kiểm tra bộ lọc và điều kiện báo cáo(165-178).
        {
            if (f == null || f.To.Date<f.From.Date || f.From.Date<new DateTime(2000, 1, 1) || 
            f.To.Date>=DateTime.MaxValue.Date || f.To.Date.Subtract(f.From.Date).TotalDays>3660)
            throw new BusinessException("Khoảng ngày không hợp lệ (tối đa 10 năm).");
            if (!new[]
            {
                "Day", "Month", "Session", "Category"
            }.Contains(f.GroupBy))
            throw new BusinessException("Chỉ nhóm theo Ngày/Tháng/Phiên/Loại hiện vật.");
            if (f.SessionId.HasValue && f.SessionId<=0 || f.CategoryId.HasValue && f.CategoryId<=0 || 
            f.ConsignorId.HasValue && f.ConsignorId<=0)
            throw new BusinessException("Bộ lọc không hợp lệ.");
        }
        private static string Label(DateTime date, string session, string category, string grouping) //Đổi lựa chọn thành nhãn báo cáo(179-184)
        {
            return grouping == "Day"?date.ToString("dd/MM/yyyy"):
            grouping == "Month"?date.ToString("yyyy-MM"):
            grouping == "Session"?session:category;
        }
        public List<BusinessReportOption> Sessions(int actor) //Lấy danh sách phiên(185-196)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REPORTS", "View");
                return db.AuctionSessions.OrderByDescending(x => x.StartsAt)
                .Select(x => new BusinessReportOption
                {
                    Id = x.SessionId, Name = x.SessionCode+" – "+x.SessionName
                }).ToList();
            }
        }
        public List<BusinessReportOption> Categories(int actor) //Truy vấn loại hiện vật.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REPORTS", "View");
                return db.Categories.OrderBy(x => x.SortOrder)
                .Select(x => new BusinessReportOption
                {
                    Id = x.CategoryId, Name = x.CategoryName
                }).ToList();
            }
        }
        public List<BusinessReportOption> Consignors(int actor) //Truy vấn chủ hàng.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REPORTS", "View");
                return db.Consignors.OrderBy(x => x.FullName)
                .Select(x => new BusinessReportOption
                {
                    Id = x.ConsignorId, Name = x.FullName
                }).ToList();
            }
        }
        // Chọn bộ dữ liệu theo loại báo cáo, không cập nhật database.
        public List<BusinessReportRow> Get(int actor, string kind, BusinessReportFilter filter) //Lấy dữ liệu theo điều kiện của Service (222-458)
        {
            Check(filter);
            if (!new[]
            {
                "Revenue", "Session", "Category", "Debt", "Payout"
            }.Contains(kind))
            throw new BusinessException("Chọn một trong năm loại báo cáo.");
            var start = filter.From.Date;
            var end = filter.To.Date.AddDays(1);
            var grouping = kind == "Session"?"Session":kind == "Category"?"Category":filter.GroupBy;
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "REPORTS", "View");
                // Khởi tạo chỉ tiêu tổng hợp từ các dòng lô; không tính lặp phí người mua.
                // khi hóa đơn có nhiều lô khác loại hay khác chủ hàng.
                var rows = new Dictionary<string, BusinessReportRow>(StringComparer.Ordinal);
                Func<string, BusinessReportRow> getRow = key => 
                {
                    BusinessReportRow row;
                    if (!rows.TryGetValue(key, out row))
                    {
                        row = new BusinessReportRow
                        {
                            GroupLabel = key
                        };
                        rows.Add(key, row);
                    }
                    return row;
                };
                if (kind == "Revenue" || kind == "Session" || kind == "Category")
                {
                    // Truy vấn toàn bộ dòng của các hóa đơn đã thanh toán, sau đó.
                    // mới lọc Category/Consignor; việc phân bổ theo tỷ trọng vẫn đủ mẫu số.
                    var receipts = db.Payments.Where(p => p.InvoiceId.HasValue && 
                    (p.PaymentType == "Invoice" || p.PaymentType == "DepositTransfer"))
                    .GroupBy(p => p.InvoiceId.Value)
                    .Select(g => new
                    {
                        Id = g.Key, PaidAt = g.Max(p => p.PaidAt)
                    })
                    .Where(x => x.PaidAt>=start && x.PaidAt<end).ToList();
                    var paidIds = receipts.Select(x => x.Id).ToList();
                    var paidDate = receipts.ToDictionary(x => x.Id, x => x.PaidAt);
                    var lines = (from detail in db.InvoiceDetails
                    join inv in db.Invoices on detail.InvoiceId equals inv.InvoiceId
                    join lot in db.AuctionLots on detail.LotId equals lot.LotId
                    join item in db.Items on lot.ItemId equals item.ItemId
                    join category in db.Categories on item.CategoryId equals category.CategoryId
                    join session in db.AuctionSessions on inv.SessionId equals session.SessionId
                    join customer in db.Customers on inv.CustomerId equals customer.CustomerId
                    where paidIds.Contains(inv.InvoiceId) && inv.InvoiceStatus == "Paid" && 
                    (!filter.SessionId.HasValue || inv.SessionId == filter.SessionId.Value) && 
                    (filter.MemberTier == null || customer.MemberTier == filter.MemberTier)
                    select new
                    {
                        inv.InvoiceId, inv.HammerSubtotal, inv.BuyerFee, inv.VipDiscount, inv.VatAmount, 
                        detail.LotId, detail.HammerPrice, lot.SellerFeeRateLocked, 
                        item.ConsignorId, item.CategoryId, session.SessionCode, session.SessionName, category.CategoryName
                    }).ToList();
                    var lotIds = lines.Select(x => x.LotId).Distinct().ToList();
                    var paidSeller = db.SellerPayouts.Where(x => lotIds.Contains(x.LotId))
                    .Select(x => new
                    {
                        x.LotId, x.SellerFee
                    }).ToList()
                    .GroupBy(x => x.LotId).ToDictionary(x => x.Key, x => x.Sum(z => z.SellerFee));
                    var seenInvoices = new Dictionary<string, HashSet<int>>();
                    foreach (var invoice in lines.GroupBy(x => x.InvoiceId))
                    {
                        var items = invoice.OrderBy(x => x.LotId).ToList();
                        var first = items[0];
                        if (first.HammerSubtotal != items.Sum(x => x.HammerPrice))
                        throw new BusinessException("Hóa đơn #"+invoice.Key+" bị lệch giá búa. Cần đối soát.");
                        decimal allocatedGross = 0, allocatedDiscount = 0, allocatedVat = 0;
                        for (int i = 0; i<items.Count; i++)
                        {
                            var x = items[i];
                            bool last = i == items.Count-1;
                            // Ở dòng cuối, cấp phần dư để tổng phân bổ đúng đến đồng xu.
                            decimal ratio = first.HammerSubtotal == 0?0:x.HammerPrice/first.HammerSubtotal;
                            decimal gross = last?first.BuyerFee-allocatedGross:Math.Round(first.BuyerFee*ratio, 2, MidpointRounding.AwayFromZero);
                            decimal discount = last?first.VipDiscount-allocatedDiscount:Math.Round(first.VipDiscount*ratio, 2, MidpointRounding.AwayFromZero);
                            decimal vat = last?first.VatAmount-allocatedVat:Math.Round(first.VatAmount*ratio, 2, MidpointRounding.AwayFromZero);
                            allocatedGross+=gross;
                            allocatedDiscount+=discount;
                            allocatedVat+=vat;
                            if (filter.CategoryId.HasValue && x.CategoryId != filter.CategoryId.Value || 
                            filter.ConsignorId.HasValue && x.ConsignorId != filter.ConsignorId.Value)continue;
                            decimal seller;
                            if (!paidSeller.TryGetValue(x.LotId, out seller))
                            {
                                if (!x.SellerFeeRateLocked.HasValue)
                                throw new BusinessException("Lot #"+x.LotId+" chưa chốt phí người bán, không thể ghi số liệu sai.");
                                seller = Math.Round(x.HammerPrice*x.SellerFeeRateLocked.Value, 2, MidpointRounding.AwayFromZero);
                            }
                            string key = Label(paidDate[x.InvoiceId], x.SessionCode+" - "+x.SessionName, x.CategoryName, grouping);
                            var row = getRow(key);
                            row.HammerTotal+=x.HammerPrice;
                            row.BuyerFee+=gross;
                            row.Discount+=discount;
                            row.SellerFee+=seller;
                            row.Vat+=vat;
                            row.PlatformRevenue+=gross-discount+seller;
                            HashSet<int> invoices;
                            if (!seenInvoices.TryGetValue(key, out invoices))
                            {
                                invoices = new HashSet<int>();
                                seenInvoices.Add(key, invoices);
                            }
                            if (invoices.Add(invoice.Key))row.InvoiceCount++;
                        }
                    }
                    // Tỷ lệ bán thành công tính theo ngày chốt lô.
                    var outcomes = (from lot in db.AuctionLots
                    join item in db.Items on lot.ItemId equals item.ItemId
                    join category in db.Categories on item.CategoryId equals category.CategoryId
                    join session in db.AuctionSessions on lot.SessionId equals session.SessionId
                    where lot.ClosesAt>=start && lot.ClosesAt<end && 
                    (!filter.SessionId.HasValue || lot.SessionId == filter.SessionId.Value) && 
                    (!filter.CategoryId.HasValue || item.CategoryId == filter.CategoryId.Value) && 
                    (!filter.ConsignorId.HasValue || item.ConsignorId == filter.ConsignorId.Value) && 
                    (lot.LotStatus == "Sold" || lot.LotStatus == "ReserveNotMet" || lot.LotStatus == "Unsold")
                    select new
                    {
                        lot.ClosesAt, lot.LotStatus, session.SessionCode, session.SessionName, category.CategoryName, 
                        lot.WinningCustomerId
                    }).ToList();
                    // Lọc hạng khách ở chỉ tiêu kết quả: lô không bán không có hạng, nên.
                    // không suy diễn tỷ lệ thành công khi lọc hạng.
                    if (filter.MemberTier == null)
                    foreach (var outcome in outcomes)
                    {
                        var key = Label(outcome.ClosesAt, outcome.SessionCode+" - "+outcome.SessionName, outcome.CategoryName, grouping);
                        var row = getRow(key);
                        if (outcome.LotStatus == "Sold")row.SoldLots++;
                        else row.UnsoldLots++;
                    }
                    return rows.Values.OrderBy(x => x.GroupLabel, StringComparer.Ordinal).ToList();
                }
                if (kind == "Debt")
                {
                    // Danh sách công nợ xét những hóa đơn chưa thanh toán đủ trong kỳ.
                    var debt = (from inv in db.Invoices
                    join session in db.AuctionSessions on inv.SessionId equals session.SessionId
                    join customer in db.Customers on inv.CustomerId equals customer.CustomerId
                    where inv.IssuedAt>=start && inv.IssuedAt<end && inv.InvoiceStatus != "Cancelled" && 
                    inv.TotalDue>inv.PaidAmount && 
                    (!filter.SessionId.HasValue || inv.SessionId == filter.SessionId.Value) && 
                    (filter.MemberTier == null || customer.MemberTier == filter.MemberTier)
                    select new
                    {
                        inv.InvoiceId, inv.IssuedAt, inv.HammerSubtotal, inv.TotalDue, inv.PaidAmount, 
                        session.SessionCode, session.SessionName
                    }).ToList();
                    var ids = debt.Select(x => x.InvoiceId).ToList();
                    var debtLines = (from detail in db.InvoiceDetails
                    join lot in db.AuctionLots on detail.LotId equals lot.LotId
                    join item in db.Items on lot.ItemId equals item.ItemId
                    join category in db.Categories on item.CategoryId equals category.CategoryId
                    where ids.Contains(detail.InvoiceId)
                    select new
                    {
                        detail.InvoiceId, detail.LotId, detail.HammerPrice, item.CategoryId, 
                        item.ConsignorId, category.CategoryName
                    }).ToList();
                    var distinctInvoices = new Dictionary<string, HashSet<int>>();
                    foreach (var invoice in debt)
                    {
                        var items = debtLines.Where(x => x.InvoiceId == invoice.InvoiceId)
                        .OrderBy(x => x.LotId).ToList();
                        if (items.Count == 0 || items.Sum(x => x.HammerPrice) != invoice.HammerSubtotal)
                        throw new BusinessException("Hóa đơn công nợ #"+invoice.InvoiceId+
                        " thiếu/không khớp chi tiết. Không được tự phân bổ công nợ.");
                        decimal remaining = invoice.TotalDue-invoice.PaidAmount;
                        decimal allocated = 0m;
                        for (int i = 0; i<items.Count; i++)
                        {
                            var x = items[i];
                            decimal part = i == items.Count-1?remaining-allocated:
                            invoice.HammerSubtotal == 0?0m:
                            Math.Round(remaining*x.HammerPrice/invoice.HammerSubtotal, 2, MidpointRounding.AwayFromZero);
                            allocated+=part;
                            if (filter.CategoryId.HasValue && x.CategoryId != filter.CategoryId.Value || 
                            filter.ConsignorId.HasValue && x.ConsignorId != filter.ConsignorId.Value)continue;
                            var key = Label(invoice.IssuedAt, invoice.SessionCode+" - "+invoice.SessionName, 
                            x.CategoryName, grouping);
                            var row = getRow(key);
                            row.UnpaidAmount+=part;
                            HashSet<int> already;
                            if (!distinctInvoices.TryGetValue(key, out already))
                            {
                                already = new HashSet<int>();
                                distinctInvoices.Add(key, already);
                            }
                            if (already.Add(invoice.InvoiceId))row.InvoiceCount++;
                        }
                    }
                    return rows.Values.OrderBy(x => x.GroupLabel).ToList();
                }
                // Báo cáo chi trả dùng ngày thực chi, không tính lệnh đang chờ.
                var payouts = (from payout in db.SellerPayouts
                join lot in db.AuctionLots on payout.LotId equals lot.LotId
                join item in db.Items on lot.ItemId equals item.ItemId
                join category in db.Categories on item.CategoryId equals category.CategoryId
                join session in db.AuctionSessions on lot.SessionId equals session.SessionId
                where payout.PayoutAt>=start && payout.PayoutAt<end && 
                (!filter.SessionId.HasValue || lot.SessionId == filter.SessionId.Value) && 
                (!filter.CategoryId.HasValue || item.CategoryId == filter.CategoryId.Value) && 
                (!filter.ConsignorId.HasValue || item.ConsignorId == filter.ConsignorId.Value)
                select new
                {
                    payout.PayoutAt, payout.HammerPrice, payout.SellerFee, 
                    payout.PayoutAmount, lot.WinningCustomerId, session.SessionCode, session.SessionName, category.CategoryName
                }).ToList();
                var customerIds = payouts.Where(x => x.WinningCustomerId.HasValue)
                .Select(x => x.WinningCustomerId.Value).Distinct().ToList();
                var tiers = db.Customers.Where(x => customerIds.Contains(x.CustomerId))
                .Select(x => new
                {
                    x.CustomerId, x.MemberTier
                }).ToList()
                .ToDictionary(x => x.CustomerId, x => x.MemberTier);
                foreach (var x in payouts)
                {
                    if (filter.MemberTier != null && (!x.WinningCustomerId.HasValue || 
                    !tiers.ContainsKey(x.WinningCustomerId.Value) || 
                    tiers[x.WinningCustomerId.Value] != filter.MemberTier))continue;
                    var key = Label(x.PayoutAt, x.SessionCode+" - "+x.SessionName, x.CategoryName, grouping);
                    var row = getRow(key);
                    row.HammerTotal+=x.HammerPrice;
                    row.SellerFee+=x.SellerFee;
                    row.PaidToSeller+=x.PayoutAmount;
                }
                return rows.Values.OrderBy(x => x.GroupLabel).ToList();
            }
        }
    }
}
