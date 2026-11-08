using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
using AuctionArena.Data.Repositories;
namespace AuctionArena.Services
{
    // Các điều kiện nghiệp vụ được kiểm tra tại Service, không chỉ tại nút bấm.
    public sealed class CatalogService : ServiceBase
    {
        public CatalogService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        // Các truy vấn danh mục đi qua Repository dùng chung DbContext/Transaction.
        static IRepository<T> Repo<T>(AuctionArenaDbContext db) where T: class //Tạo repository từ context hiện tại.
        {
            return new EfRepository<T>(db);
        }
        // Dữ liệu danh mục được kiểm tra quyền và điều kiện trước khi lưu.
        public List<CustomerEntity> Customers(int actor, string text = "", string tier = null, string status = null) //Truy vấn khách hàng(22-36).
        {
            text = (text??"").Trim();
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CUSTOMERS", "View");
                var q = Repo<CustomerEntity>(db).Query().AsNoTracking().Where(x => x.CustomerCode.Contains(text) || 
                x.FullName.Contains(text) || x.Phone.Contains(text) || x.IdentityNumber.Contains(text));
                if (!string.IsNullOrWhiteSpace(tier))q = q.Where(x => x.MemberTier == tier);
                if (status == "Blacklist")q = q.Where(x => x.IsBlacklisted);
                else if (status == "Inactive")q = q.Where(x => x.CustomerStatus == "Inactive");
                else if (status == "Active")q = q.Where(x => x.CustomerStatus != "Inactive" && !x.IsBlacklisted);
                return q.OrderBy(x => x.FullName).ToList();
            }
        }
        static void CheckContact(string phone, string email, string identity) //Kiểm tra dữ liệu liên hệ
        {
            phone = (phone??"").Trim();
            email = (email??"").Trim();
            identity = (identity??"").Trim();
            if (phone.Length>0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[0-9]{9}$"))
            throw new BusinessException("Số điện thoại cần 10 số và bắt đầu bằng 0.");
            if (identity.Length>0 && !System.Text.RegularExpressions.Regex.IsMatch(identity, @"^([0-9]{9}|[0-9]{12})$"))
            throw new BusinessException("CCCD/CMND phải có 9 hoặc 12 chữ số.");
            if (email.Length>0 && (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") || email.Length>150))
            throw new BusinessException("Email không đúng định dạng.");
        }
        public void SaveCustomer(int actor, CustomerEntity x) //Lưu hồ sơ khách và các thay đổi hạng/ảnh(49-131).
        {
            if (x == null || string.IsNullOrWhiteSpace(x.FullName) || x.FullName.Trim().Length>120)
            throw new BusinessException("Họ tên khách hàng cần từ 1 đến 120 ký tự.");
            if (!new[]
            {
                "Normal", "Silver", "Gold", "VIP"
            }.Contains(x.MemberTier))
            throw new BusinessException("Hạng khách phải là Thường, Bạc, Vàng hoặc VIP.");
            if (x.DateOfBirth.HasValue && (x.DateOfBirth.Value.Date>DateTime.Today || 
            x.DateOfBirth.Value.Date<DateTime.Today.AddYears(-120)))
            throw new BusinessException("Ngày sinh không hợp lệ.");
            if (!string.IsNullOrWhiteSpace(x.Gender) && !new[]
            {
                "Male", "Female", "Other"
            }.Contains(x.Gender))
            throw new BusinessException("Giới tính không hợp lệ.");
            if (x.PhotoData != null && x.PhotoData.Length>2*1024*1024)
            throw new BusinessException("Ảnh khách không được vượt 2 MB.");
            if ((x.Notes??"").Length>500)
            {
                throw new BusinessException("Ghi chú không quá 500 ký tự.");
            }
            CheckContact(x.Phone, x.Email, x.IdentityNumber);
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "CUSTOMERS", x.CustomerId == 0?"Create":"Edit");
                    var code = string.IsNullOrWhiteSpace(x.CustomerCode)?Code("KH"):x.CustomerCode.Trim();
                    if (code.Length>30 || db.Customers.Any(c => c.CustomerCode == code && c.CustomerId != x.CustomerId))
                    throw new BusinessException("Mã khách hàng bị trùng hoặc quá dài.");
                    if (!string.IsNullOrWhiteSpace(x.IdentityNumber) && db.Customers.Any(c => c.IdentityNumber == x.IdentityNumber && c.CustomerId != x.CustomerId))
                    throw new BusinessException("CCCD đã tồn tại ở khách hàng khác.");
                    var customer = x.CustomerId == 0?new CustomerEntity
                    {
                        CreatedAt = Now(), CustomerStatus = "Active"
                    }
                    :
                    db.Customers.Single(c => c.CustomerId == x.CustomerId);
                    string oldTier = customer.MemberTier;
                    if (x.CustomerId == 0)
                    {
                        customer.CustomerCode = code;
                        db.Customers.Add(customer);
                    }
                    customer.FullName = x.FullName.Trim();
                    customer.Phone = (x.Phone??"").Trim();
                    customer.Email = (x.Email??"").Trim();
                    customer.IdentityNumber = (x.IdentityNumber??"").Trim();
                    customer.Address = x.Address;
                    customer.Gender = x.Gender;
                    customer.DateOfBirth = x.DateOfBirth;
                    customer.MemberTier = x.MemberTier;
                    customer.Notes = x.Notes;
                    if (x.CustomerId == 0)
                    {
                        customer.IsBlacklisted = false;
                        customer.HasOutstandingDebt = false;
                    }
                    if (x.PhotoData != null)customer.PhotoData = x.PhotoData;
                    db.SaveChanges();
                    if (x.CustomerId != 0 && !string.Equals(oldTier, customer.MemberTier, StringComparison.Ordinal))
                    {
                        db.CustomerTierHistory.Add(new CustomerTierHistoryEntity
                        {
                            CustomerId = customer.CustomerId, 
                            OldTier = oldTier, NewTier = customer.MemberTier, ChangedByUserId = actor, ChangedAt = Now()
                        });
                        Audit(db, actor, "CUSTOMER_TIER", "Customers", customer.CustomerId, 
                        "Đổi hạng: "+(oldTier??"(mới)")+" → "+customer.MemberTier);
                    }
                    Audit(db, actor, "CUSTOMER_SAVE", "Customers", customer.CustomerId, "Lưu hồ sơ khách hàng "+code);
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
        public void RemoveCustomerPhoto(int actor, int customerId) //Xóa ảnh khách trong hồ sơ(132-152)
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "CUSTOMERS", "Edit");
                    var c = db.Customers.Single(x => x.CustomerId == customerId);
                    c.PhotoData = null;
                    c.PhotoPath = null;
                    Audit(db, actor, "CUSTOMER_PHOTO_REMOVE", "Customers", customerId, "Xóa ảnh khách hàng");
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
        public void SetBlacklist(int actor, int customerId, bool blocked, string reason) //Đặt hoặc gỡ trạng thái hạn chế.(153-180)
        {
            reason = (reason??"").Trim();
            if (reason.Length<5 || reason.Length>300)
            throw new BusinessException("Lý do đưa vào/gỡ blacklist phải từ 5 đến 300 ký tự.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "CUSTOMERS", "Edit");
                    var customer = db.Customers.Single(x => x.CustomerId == customerId);
                    if (customer.IsBlacklisted == blocked)
                    {
                        throw new BusinessException("Trạng thái blacklist không thay đổi.");
                    }
                    customer.IsBlacklisted = blocked;
                    customer.BlacklistReason = blocked?reason:null;
                    Audit(db, actor, blocked?"CUSTOMER_BLACKLIST":"CUSTOMER_UNBLACKLIST", "Customers", customerId, reason);
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
        public List<CustomerTierHistoryEntity> CustomerTierChanges(int actor, int customerId)//Lấy lịch sử hạng khách.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CUSTOMERS", "View");
                return Repo<CustomerTierHistoryEntity>(db).Query().AsNoTracking().Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.ChangedAt).ToList();
            }
        }
        public CustomerParticipation CustomerSummary(int actor, int id)//Tổng hợp hồ sơ và số liệu khách(190-209)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CUSTOMERS", "View");
                if (!db.Customers.Any(x => x.CustomerId == id))
                {
                    throw new BusinessException("Không tìm thấy khách hàng.");
                }
                var regs = db.Registrations.Where(x => x.CustomerId == id).ToList();
                var invoices = db.Invoices.Where(x => x.CustomerId == id).ToList();
                return new CustomerParticipation
                {
                    Registrations = regs.Count, WonLots = db.AuctionLots.Count(x => x.WinningCustomerId == id), 
                    Invoices = invoices.Count, TotalSpent = invoices.Sum(x => x.PaidAmount+x.DepositApplied), 
                    RemainingDebt = invoices.Where(x => x.InvoiceStatus != "Cancelled").Sum(x => Math.Max(0m, x.TotalDue-x.PaidAmount)), 
                    HeldDeposit = regs.Sum(x => Math.Max(0m, x.DepositPaid-x.DepositApplied-x.DepositRefunded-x.DepositForfeited))
                };
            }
        }
        public List<ConsignorEntity> Consignors(int actor, string text = "", string type = null, string status = null)//Truy vấn chủ hàng
        {
            text = (text??"").Trim();
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONSIGNORS", "View");
                var q = Repo<ConsignorEntity>(db).Query().AsNoTracking().Where(x => x.ConsignorCode.Contains(text) || x.FullName.Contains(text) || 
                x.Phone.Contains(text) || x.IdentityNumber.Contains(text));
                if (!string.IsNullOrEmpty(type))q = q.Where(x => x.ConsignorType == type);
                if (!string.IsNullOrEmpty(status))q = q.Where(x => x.ConsignorStatus == status);
                return q.OrderBy(x => x.FullName).ToList();
            }
        }
        public List<ConsignorEntity> ConsignorOptions(int actor, string permission)//Lấy chủ hàng cho danh sách chọn.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, permission, "View");
                return Repo<ConsignorEntity>(db).Query().AsNoTracking().Where(x => x.ConsignorStatus != "Inactive")
                .OrderBy(x => x.FullName).ToList();
            }
        }
        public void SaveConsignor(int actor, ConsignorEntity x)//Lưu hồ sơ chủ hàng(232-291).
        {
            if (x == null || string.IsNullOrWhiteSpace(x.FullName) || x.FullName.Length>120 || 
            !new[]
            {
                "Individual", "Organization"
            }.Contains(x.ConsignorType))
            throw new BusinessException("Tên hoặc loại chủ hàng không hợp lệ.");
            if (x.SellerFeeRate.HasValue && (x.SellerFeeRate<0m || x.SellerFeeRate>1m))
            throw new BusinessException("Phí chủ hàng phải từ 0 đến 100%.");
            CheckContact(x.Phone, x.Email, null);
            if (!string.IsNullOrWhiteSpace(x.IdentityNumber) && 
            !System.Text.RegularExpressions.Regex.IsMatch(x.IdentityNumber, @"^([0-9]{9}|[0-9]{10}|[0-9]{12}|[0-9]{13})$"))
            throw new BusinessException("CCCD/MST phải gồm 9, 10, 12 hoặc 13 chữ số.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "CONSIGNORS", x.ConsignorId == 0?"Create":"Edit");
                    var code = string.IsNullOrWhiteSpace(x.ConsignorCode)?Code("CH"):x.ConsignorCode.Trim();
                    if (code.Length>30 || db.Consignors.Any(other => other.ConsignorCode == code && other.ConsignorId != x.ConsignorId))
                    throw new BusinessException("Mã chủ hàng trùng.");
                    if (!string.IsNullOrWhiteSpace(x.IdentityNumber) && 
                    db.Consignors.Any(other => other.IdentityNumber == x.IdentityNumber && other.ConsignorId != x.ConsignorId))
                    throw new BusinessException("CCCD/MST đã tồn tại.");
                    var c = x.ConsignorId == 0?new ConsignorEntity
                    {
                        ConsignorStatus = "Active"
                    }
                    :
                    db.Consignors.Single(z => z.ConsignorId == x.ConsignorId);
                    if (x.ConsignorId == 0)
                    {
                        c.ConsignorCode = code;
                        db.Consignors.Add(c);
                    }
                    c.FullName = x.FullName.Trim();
                    c.ConsignorType = x.ConsignorType;
                    c.RepresentativeName = x.RepresentativeName;
                    c.Phone = x.Phone;
                    c.Email = x.Email;
                    c.IdentityNumber = x.IdentityNumber;
                    c.Address = x.Address;
                    c.BankName = x.BankName;
                    c.BankAccount = x.BankAccount;
                    c.BankAccountHolder = x.BankAccountHolder;
                    c.SellerFeeRate = x.SellerFeeRate;
                    c.Notes = x.Notes;
                    db.SaveChanges();
                    Audit(db, actor, "CONSIGNOR_SAVE", "Consignors", c.ConsignorId, "Lưu chủ hàng "+code);
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
        public ConsignorPortfolio ConsignorSummary(int actor, int id)//Tổng hợp số liệu chủ hàng(292-320).
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONSIGNORS", "View");
                if (!db.Consignors.Any(x => x.ConsignorId == id))
                {
                    throw new BusinessException("Không tìm thấy chủ hàng.");
                }
                var contracts = db.ConsignmentContracts.Where(x => x.ConsignorId == id).ToList();
                var items = db.Items.Where(x => x.ConsignorId == id).ToList();
                var payouts = db.SellerPayouts.Where(x => x.ConsignorId == id).ToList();
                var soldLots = (from l in db.AuctionLots join i in db.Items on l.ItemId equals i.ItemId
                join d in db.InvoiceDetails on l.LotId equals d.LotId
                join invoice in db.Invoices on d.InvoiceId equals invoice.InvoiceId
                where i.ConsignorId == id && l.LotStatus == "Sold" && invoice.InvoiceStatus == "Paid"
                select new
                {
                    l.LotId, l.HammerPrice, l.SellerFeeRateLocked
                }).ToList();
                decimal paid = payouts.Sum(x => x.PayoutAmount);
                decimal due = soldLots.Sum(x => (x.HammerPrice??0m)*(1m-(x.SellerFeeRateLocked??0m)));
                return new ConsignorPortfolio
                {
                    Contracts = contracts.Count, Items = items.Count, 
                    Paid = paid, Outstanding = Math.Max(0m, due-paid)
                };
            }
        }
        public List<ConsignorContractRow> ConsignorContracts(int actor, int consignorId)//Lấy hợp đồng của chủ hàng
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONSIGNORS", "View");
                return db.ConsignmentContracts.AsNoTracking().Where(x => x.ConsignorId == consignorId)
                .OrderByDescending(x => x.SignedAt).Select(x => new ConsignorContractRow
                {
                    Code = x.ContractCode, SignedAt = x.SignedAt, Status = x.ContractStatus
                }).ToList();
            }
        }
        public List<ConsignorItemRow> ConsignorItems(int actor, int consignorId)//Lấy hiện vật của chủ hàng.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONSIGNORS", "View");
                return Repo<ItemEntity>(db).Query().AsNoTracking().Where(x => x.ConsignorId == consignorId)
                .OrderBy(x => x.ItemCode).Select(x => new ConsignorItemRow
                {
                    Code = x.ItemCode, Name = x.ItemName, Status = x.ItemStatus, Estimated = x.EstimatePrice
                }).ToList();
            }
        }
        public List<ConsignorPayoutRow> ConsignorPayouts(int actor, int consignorId)//Lấy chi trả cho chủ hàng
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONSIGNORS", "View");
                return db.SellerPayouts.AsNoTracking().Where(x => x.ConsignorId == consignorId)
                .OrderByDescending(x => x.PayoutAt).Select(x => new ConsignorPayoutRow
                {
                    Code = x.PayoutCode, At = x.PayoutAt, Amount = x.PayoutAmount
                }).ToList();
            }
        }
        public List<CustomerParticipationRow> CustomerActivity(int actor, int customerId)//Lấy lịch sử hoạt động khách(357-388)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CUSTOMERS", "View");
                var rows = (from reg in db.Registrations join ses in db.AuctionSessions on reg.SessionId equals ses.SessionId
                where reg.CustomerId == customerId
                select new CustomerParticipationRow
                {
                    Code = ses.SessionCode, 
                    When = reg.RegisteredAt, Type = "Đăng ký phiên", Amount = reg.DepositPaid, 
                    Status = reg.RegistrationStatus
                }).ToList();
                var bills = db.Invoices.Where(x => x.CustomerId == customerId).Select(x => new CustomerParticipationRow
                {
                    Code = x.InvoiceCode, When = x.IssuedAt, Type = "Hóa đơn", Amount = x.TotalDue, Status = x.InvoiceStatus
                }).ToList();
                rows.AddRange(bills);
                var wins = db.AuctionLots.Where(x => x.WinningCustomerId == customerId && x.LotStatus == "Sold")
                .Select(x => new
                {
                    x.LotId, x.ClosesAt, x.HammerPrice
                }).ToList()
                .Select(x => new CustomerParticipationRow
                {
                    Code = "Lot #"+x.LotId, When = x.ClosesAt, 
                    Type = "Lot đã thắng", Amount = x.HammerPrice??0m, Status = "Sold"
                }).ToList();
                rows.AddRange(wins);
                return rows.OrderByDescending(x => x.When).ToList();
            }
        }
        public List<CategorieEntity> Categories(int actor, string text = "", string permission = "CATEGORIES", bool activeOnly = false)
        {//Truy vấn loại hiện vật
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, permission, "View");
                var q = Repo<CategorieEntity>(db).Query().AsNoTracking().Where(x => x.CategoryName.Contains(text) || x.CategoryCode.Contains(text));
                if (activeOnly)q = q.Where(x => x.IsActive);
                return q.OrderBy(x => x.SortOrder).ThenBy(x => x.CategoryName).ToList();
            }
        }
        public int CategoryItemCount(int actor, int id)//Đếm hiện vật theo loại.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CATEGORIES", "View");
                return db.Items.Count(x => x.CategoryId == id);
            }
        }
        public void SaveCategory(int actor, CategorieEntity x)//Lưu loại hiện vật(404-447)
        {
            if (x == null || string.IsNullOrWhiteSpace(x.CategoryName) || x.CategoryName.Trim().Length>100 || x.SortOrder<0)
            throw new BusinessException("Tên hoặc thứ tự hiển thị không hợp lệ.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "CATEGORIES", x.CategoryId == 0?"Create":"Edit");
                    string name = x.CategoryName.Trim();
                    string code = string.IsNullOrWhiteSpace(x.CategoryCode)?Code("LOAI"):x.CategoryCode.Trim();
                    if (code.Length>30 || db.Categories.Any(z => z.CategoryCode == code && z.CategoryId != x.CategoryId))
                    throw new BusinessException("Mã loại hiện vật trùng.");
                    if (db.Categories.Any(z => z.CategoryId != x.CategoryId && z.CategoryName.ToLower() == name.ToLower()))
                    throw new BusinessException("Tên loại hiện vật đã tồn tại (không phân biệt hoa thường).");
                    var c = x.CategoryId == 0?new CategorieEntity
                    {
                        IsActive = true
                    }
                    :db.Categories.Single(z => z.CategoryId == x.CategoryId);
                    if (x.CategoryId == 0)
                    {
                        c.CategoryCode = code;
                        db.Categories.Add(c);
                    }
                    c.CategoryName = name;
                    c.Description = x.Description;
                    c.SortOrder = x.SortOrder;
                    if (x.CategoryId != 0)c.IsActive = x.IsActive;
                    db.SaveChanges();
                    Audit(db, actor, "CATEGORY_SAVE", "Categories", c.CategoryId, "Lưu loại hiện vật "+code);
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
        public List<ItemEntity> Items(int actor, string text = "", int? categoryId = null, int? consignorId = null, //Truy vấn hiện vật(448-465)
        string status = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            text = (text??"").Trim();
            if (minPrice.HasValue && maxPrice.HasValue && minPrice>maxPrice)
            throw new BusinessException("Giá lọc tối thiểu không được lớn hơn tối đa.");
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ITEMS", "View");
                var q = Repo<ItemEntity>(db).Query().AsNoTracking().Where(x => x.ItemCode.Contains(text) || x.ItemName.Contains(text));
                if (categoryId.HasValue)q = q.Where(x => x.CategoryId == categoryId.Value);
                if (consignorId.HasValue)q = q.Where(x => x.ConsignorId == consignorId.Value);
                if (!string.IsNullOrWhiteSpace(status))q = q.Where(x => x.ItemStatus == status);
                if (minPrice.HasValue)q = q.Where(x => x.EstimatePrice>=minPrice.Value);
                if (maxPrice.HasValue)q = q.Where(x => x.EstimatePrice<=maxPrice.Value);
                return q.OrderByDescending(x => x.CreatedAt).ToList();
            }
        }
        public List<ItemEntity> PendingItemsForContract(int actor, int consignorId)//Lấy hiện vật có thể lập hợp đồng
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONTRACTS", "View");
                return Repo<ItemEntity>(db).Query().AsNoTracking().Where(x => x.ConsignorId == consignorId && x.ItemStatus == "Approved" && 
                !db.ConsignmentContractDetails.Any(d => d.ItemId == x.ItemId))
                .OrderBy(x => x.ItemCode).ToList();
            }
        }
        public void SaveItem(int actor, ItemEntity x)//Lưu hồ sơ hiện vật(476-548)
        {
            if (x == null || string.IsNullOrWhiteSpace(x.ItemName) || x.ItemName.Length>160)
            throw new BusinessException("Tên hiện vật không hợp lệ.");
            ValidateMoneyOrZero(x.EstimatePrice);
            if (x.EstimatePriceHigh.HasValue)
            {
                ValidateMoneyOrZero(x.EstimatePriceHigh.Value);
                if (x.EstimatePriceHigh.Value<x.EstimatePrice)
                throw new BusinessException("Giá ước tính cao phải lớn hơn hoặc bằng giá thấp.");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "ITEMS", x.ItemId == 0?"Create":"Edit");
                    var code = string.IsNullOrWhiteSpace(x.ItemCode)?Code("HV"):x.ItemCode.Trim();
                    if (code.Length>30 || db.Items.Any(z => z.ItemCode == code && z.ItemId != x.ItemId))
                    throw new BusinessException("Mã hiện vật đã tồn tại hoặc quá dài.");
                    var category = db.Categories.SingleOrDefault(z => z.CategoryId == x.CategoryId);
                    var consignor = db.Consignors.SingleOrDefault(z => z.ConsignorId == x.ConsignorId);
                    if (category == null || consignor == null || 
                    (x.ItemId == 0 && (!category.IsActive || consignor.ConsignorStatus == "Inactive")))
                    throw new BusinessException("Loại hiện vật hoặc chủ hàng không còn hoạt động.");
                    var item = x.ItemId == 0?new ItemEntity
                    {
                        CreatedAt = Now(), ItemStatus = "PendingAppraisal"
                    }
                    :
                    db.Items.Single(z => z.ItemId == x.ItemId);
                    if (x.ItemId == 0)
                    {
                        item.ItemCode = code;
                        db.Items.Add(item);
                    }
                    else
                    {
                        if (item.ItemStatus != "PendingAppraisal" && item.ItemStatus != "NeedInfo" && 
                        item.ItemStatus != "Rejected" && item.ItemStatus != "Approved")
                        throw new BusinessException("Chỉ sửa hồ sơ khi chưa được duyệt thẩm định.");
                        if (db.ConsignmentContractDetails.Any(z => z.ItemId == x.ItemId) || 
                        db.AuctionLots.Any(z => z.ItemId == x.ItemId) || db.StockReceiptDetails.Any(z => z.ItemId == x.ItemId))
                        throw new BusinessException("Hiện vật có chứng từ: không được sửa hồ sơ gốc.");
                    }
                    item.ItemName = x.ItemName.Trim();
                    item.CategoryId = x.CategoryId;
                    item.ConsignorId = x.ConsignorId;
                    item.Description = x.Description;
                    item.Origin = x.Origin;
                    item.Era = x.Era;
                    item.Dimensions = x.Dimensions;
                    item.Material = x.Material;
                    item.ConditionDescription = x.ConditionDescription;
                    item.EstimatePrice = x.EstimatePrice;
                    item.EstimatePriceHigh = x.EstimatePriceHigh;
                    // Vị trí kho do mục Phiếu nhập kho quản lý, không ghi từ màn hiện vật.
                    db.SaveChanges();
                    Audit(db, actor, "ITEM_SAVE", "Items", item.ItemId, "Lưu hồ sơ hiện vật "+code);
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Dữ liệu hiện vật đã thay đổi, hãy tải lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public void Appraise(int actor, int itemId, decimal? price, string decision, string notes)//Lưu kết quả thẩm định và trạng thái(549-604).
        {
            decision = (decision??"").Trim();
            notes = (notes??"").Trim();
            if (!new[]
            {
                "Approved", "Rejected", "NeedInfo"
            }.Contains(decision))
            throw new BusinessException("Kết quả thẩm định không hợp lệ.");
            if (decision != "Approved" && notes.Length<5)
            throw new BusinessException("Từ chối/yêu cầu bổ sung phải ghi lý do ít nhất 5 ký tự.");
            if (notes.Length>400)
            {
                throw new BusinessException("Ghi chú thẩm định tối đa 400 ký tự.");
            }
            if (decision == "Approved")
            {
                if (!price.HasValue)
                {
                    throw new BusinessException("Phải nhập giá thẩm định khi duyệt.");
                }
                ValidateMoney(price.Value);
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "ITEMS", "Edit");
                    var item = db.Items.SingleOrDefault(x => x.ItemId == itemId);
                    if (item == null || !new[]
                    {
                        "PendingAppraisal", "NeedInfo", "Rejected"
                    }.Contains(item.ItemStatus))
                    throw new BusinessException("Không thể thẩm định hiện vật đã chuyển sang nghiệp vụ khác.");
                    // Một số hợp đồng có hiện vật chưa được ghi nhận kết quả thẩm định.
                    // Cho thẩm định trước khi nhập kho mà không sửa giao dịch liên quan.
                    if (db.StockReceiptDetails.Any(x => x.ItemId == itemId) || 
                    db.AuctionLots.Any(x => x.ItemId == itemId))
                    throw new BusinessException("Hiện vật đã nhập kho/xếp lot, không sửa kết quả thẩm định.");
                    item.AppraisedPrice = decision == "Approved"?price:null;
                    item.ItemStatus = decision;
                    item.AppraisalNotes = notes;
                    item.AppraisedAt = Now();
                    item.AppraisedByUserId = actor;
                    Audit(db, actor, "ITEM_APPRAISE", "Items", itemId, 
                    "Chuyển trạng thái "+decision+"; "+notes);
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
        public List<ItemJourneyRow> ItemJourney(int actor, int itemId)//Tổng hợp hành trình hiện vật qua các bước nghiệp vụ(605-670)
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ITEMS", "View");
                var item = db.Items.AsNoTracking().SingleOrDefault(x => x.ItemId == itemId);
                if (item == null)
                {
                    throw new BusinessException("Hiện vật không tồn tại.");
                }
                var rows = new List<ItemJourneyRow>
                {
                    new ItemJourneyRow
                    {
                        At = item.CreatedAt, 
                        Phase = "Tiếp nhận hồ sơ", Detail = item.ItemCode
                    }
                };
                if (item.AppraisedAt.HasValue)rows.Add(new ItemJourneyRow
                {
                    At = item.AppraisedAt.Value, 
                    Phase = "Thẩm định", Detail = (item.AppraisalNotes??"")
                });
                var contracts = (from detail in db.ConsignmentContractDetails join c in db.ConsignmentContracts
                on detail.ContractId equals c.ContractId where detail.ItemId == itemId
                select new
                {
                    c.SignedAt, c.ContractCode
                }).ToList();
                rows.AddRange(contracts.Select(x => new ItemJourneyRow
                {
                    At = x.SignedAt, Phase = "Ký gửi", Detail = x.ContractCode
                }));
                var receipts = (from d in db.StockReceiptDetails join receipt in db.StockReceipts
                on d.ReceiptId equals receipt.ReceiptId where d.ItemId == itemId
                select new
                {
                    receipt.ReceivedAt, receipt.ReceiptCode
                }).ToList();
                rows.AddRange(receipts.Select(x => new ItemJourneyRow
                {
                    At = x.ReceivedAt, Phase = "Nhập kho", Detail = x.ReceiptCode
                }));
                var lots = (from l in db.AuctionLots join a in db.AuctionSessions on l.SessionId equals a.SessionId
                where l.ItemId == itemId select new
                {
                    l.OpensAt, a.SessionCode, l.LotStatus, l.LotNumber
                }).ToList();
                rows.AddRange(lots.Select(x => new ItemJourneyRow
                {
                    At = x.OpensAt, Phase = "Lot/phiên", 
                    Detail = x.SessionCode+" #"+x.LotNumber+" – "+x.LotStatus
                }));
                var issues = (from d in db.StockIssueDetails join issue in db.StockIssues
                on d.IssueId equals issue.IssueId where d.ItemId == itemId
                select new
                {
                    issue.IssuedAt, issue.IssueCode
                }).ToList();
                rows.AddRange(issues.Select(x => new ItemJourneyRow
                {
                    At = x.IssuedAt, Phase = "Xuất kho", Detail = x.IssueCode
                }));
                return rows.OrderBy(x => x.At).ToList();
            }
        }
        // Xóa bản ghi danh mục chưa phát sinh chứng từ. Không xóa lịch sử giao dịch.
        public void DeleteCustomer(int actor, int id)//Kiểm tra tham chiếu trước khi xóa khách(672-711).
        {
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "CUSTOMERS", "Delete");
                    if (db.Registrations.Any(x => x.CustomerId == id) || db.Bids.Any(x => x.CustomerId == id) || 
                    db.Invoices.Any(x => x.CustomerId == id) || db.CustomerViolations.Any(x => x.CustomerId == id) || 
                    db.AuctionLots.Any(x => x.LeadingCustomerId == id || x.WinningCustomerId == id) || 
                    db.CustomerTierHistory.Any(x => x.CustomerId == id))
                    {
                        var used = db.Customers.SingleOrDefault(x => x.CustomerId == id);
                        if (used == null)
                        {
                            throw new BusinessException("Không tìm thấy khách hàng.");
                        }
                        used.CustomerStatus = "Inactive";
                        Audit(db, actor, "CUSTOMER_INACTIVE", "Customers", id, "Ngừng sử dụng vì đã phát sinh chứng từ");
                        db.SaveChanges();
                        tx.Commit();
                        return;
                    }
                    var customer = db.Customers.SingleOrDefault(x => x.CustomerId == id);
                    if (customer == null)
                    {
                        throw new BusinessException("Không tìm thấy khách hàng.");
                    }
                    db.Customers.Remove(customer);
                    Audit(db, actor, "CUSTOMER_DELETE", "Customers", id, "Xóa khách chưa có giao dịch");
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
        public void DeleteConsignor(int actor, int id)//Kiểm tra tham chiếu trước khi xóa chủ hàng(712-749).
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "CONSIGNORS", "Delete");
                    if (db.Items.Any(x => x.ConsignorId == id) || 
                    db.ConsignmentContracts.Any(x => x.ConsignorId == id) || db.SellerPayouts.Any(x => x.ConsignorId == id))
                    {
                        var used = db.Consignors.SingleOrDefault(x => x.ConsignorId == id);
                        if (used == null)
                        {
                            throw new BusinessException("Không tìm thấy chủ hàng.");
                        }
                        used.ConsignorStatus = "Inactive";
                        Audit(db, actor, "CONSIGNOR_INACTIVE", "Consignors", id, "Ngừng hợp tác vì đã có chứng từ");
                        db.SaveChanges();
                        tx.Commit();
                        return;
                    }
                    var seller = db.Consignors.SingleOrDefault(x => x.ConsignorId == id);
                    if (seller == null)
                    {
                        throw new BusinessException("Không tìm thấy chủ hàng.");
                    }
                    db.Consignors.Remove(seller);
                    Audit(db, actor, "CONSIGNOR_DELETE", "Consignors", id, "Xóa chủ hàng chưa có giao dịch");
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
        public void DeleteCategory(int actor, int id) //Kiểm tra tham chiếu trước khi xóa loại(750-786).
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "CATEGORIES", "Delete");
                    if (db.Items.Any(x => x.CategoryId == id))
                    {
                        var used = db.Categories.SingleOrDefault(x => x.CategoryId == id);
                        if (used == null)
                        {
                            throw new BusinessException("Không tìm thấy loại hiện vật.");
                        }
                        used.IsActive = false;
                        Audit(db, actor, "CATEGORY_INACTIVE", "Categories", id, "Ngừng sử dụng vì đã có hiện vật");
                        db.SaveChanges();
                        tx.Commit();
                        return;
                    }
                    var category = db.Categories.SingleOrDefault(x => x.CategoryId == id);
                    if (category == null)
                    {
                        throw new BusinessException("Không tìm thấy loại hiện vật.");
                    }
                    db.Categories.Remove(category);
                    Audit(db, actor, "CATEGORY_DELETE", "Categories", id, "Xóa loại hiện vật chưa sử dụng");
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
        public void DeleteItem(int actor, int id)//Kiểm tra trạng thái/tham chiếu trước khi xóa hiện vật(787-815).
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "ITEMS", "Delete");
                    var item = db.Items.SingleOrDefault(x => x.ItemId == id);
                    if (item == null)
                    {
                        throw new BusinessException("Không tìm thấy hiện vật.");
                    }
                    if (item.ItemStatus != "Approved" || 
                    db.ConsignmentContractDetails.Any(x => x.ItemId == id) || 
                    db.StockReceiptDetails.Any(x => x.ItemId == id) || db.AuctionLots.Any(x => x.ItemId == id) || 
                    db.StockIssueDetails.Any(x => x.ItemId == id) || db.ItemImages.Any(x => x.ItemId == id))
                    throw new BusinessException("Hiện vật đã có chứng từ, ảnh hoặc không còn chờ thẩm định; không thể xóa.");
                    db.Items.Remove(item);
                    Audit(db, actor, "ITEM_DELETE", "Items", id, "Xóa hiện vật chưa có chứng từ");
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
    public sealed class ConsignmentService : ServiceBase
    {
        public ConsignmentService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {//Khởi tạo ConsignmentService. Nhận phụ thuộc/cấu hình ban đầu
        }
        // Ký hợp đồng và nhập hàng vào kho là hai thao tác khác nhau.
        public int ReceiveContract(int actor, int consignorId, IList<int> itemIds, string notes, //Tiếp nhận hợp đồng và chi tiết hiện vật(823-887)
        decimal? feeRate = null, DateTime? expiry = null, DateTime? signedAt = null)
        {
            DateTime signed = signedAt.HasValue ? signedAt.Value.Date : Now();
            if (signed.Date > DateTime.Today || signed.Date < new DateTime(2000, 1, 1))
            throw new BusinessException("Ngày ký không hợp lệ hoặc ở tương lai.");
            if ((feeRate.HasValue && (feeRate.Value < 0m || feeRate.Value > 1m)) || 
            (expiry.HasValue && expiry.Value.Date < signed.Date))
            throw new BusinessException("Phí từ 0 đến 100%; ngày hết hạn không được trước ngày ký.");
            if (itemIds == null || itemIds.Count == 0 || itemIds.Distinct().Count() != itemIds.Count)
            {
                throw new BusinessException("Phải chọn ít nhất một hiện vật, không được trùng.");
            }
            using (var db = Factory()) using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "CONTRACTS", "Create");
                    if (!db.Consignors.Any(x => x.ConsignorId == consignorId))
                    {
                        throw new BusinessException("Chủ hàng không tồn tại.");
                    }
                    var items = db.Items.Where(x => itemIds.Contains(x.ItemId)).ToList();
                    if (items.Count != itemIds.Count || items.Any(x => x.ConsignorId != consignorId || x.ItemStatus != "Approved"))
                    throw new BusinessException("Chỉ hiện vật đã được duyệt thẩm định của đúng chủ hàng mới được ký gửi.");
                    if (items.Any(x => db.ConsignmentContractDetails.Any(d => d.ItemId == x.ItemId)))
                    {
                        throw new BusinessException("Một hiện vật đã có hợp đồng ký gửi.");
                    }
                    var seller = db.Consignors.Single(x => x.ConsignorId == consignorId);
                    // Chốt phí tại thời điểm ký, không lấy lại theo cấu hình khi chi trả.
                    var agreedRate = feeRate ?? seller.SellerFeeRate ?? RateSetting(db, "SellerFeeRate");
                    if (agreedRate<0m || agreedRate>1m)
                    throw new BusinessException("Tỷ lệ phí người bán phải từ 0 đến 100%.");
                    agreedRate = Math.Round(agreedRate, 6, MidpointRounding.AwayFromZero);
                    var contract = new ConsignmentContractEntity
                    {
                        ContractCode = Code("KG"), ConsignorId = consignorId, 
                        SignedAt = signed, ContractStatus = "Active", Notes = notes, SellerFeeRate = agreedRate, ExpiresAt = expiry
                    };
                    db.ConsignmentContracts.Add(contract);
                    db.SaveChanges();
                    foreach (var item in items)
                    db.ConsignmentContractDetails.Add(new ConsignmentContractDetailEntity
                    {
                        ContractId = contract.ContractId, ItemId = item.ItemId
                    });
                    Audit(db, actor, "CONTRACT_SIGN", "ConsignmentContracts", contract.ContractId, 
                    "Ký hợp đồng " + items.Count + " hiện vật; chưa nhập kho, chờ thủ kho xác nhận");
                    db.SaveChanges();
                    tx.Commit();
                    return contract.ContractId;
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Dữ liệu kho đã thay đổi, hãy tải lại.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Chỉ hiện các hiện vật theo hợp đồng chưa được ghi nhận nhập kho.
        public List<ReceiptContractOption> ContractsAwaitingReceipt(int actor)//Lấy hợp đồng còn chờ nhập kho.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_RECEIPTS", "View");
                return (from contract in db.ConsignmentContracts
                join seller in db.Consignors on contract.ConsignorId equals seller.ConsignorId
                where contract.ContractStatus == "Active" && 
                db.ConsignmentContractDetails.Any(d => d.ContractId == contract.ContractId && 
                !db.StockReceiptDetails.Any(rd => rd.ItemId == d.ItemId))
                orderby contract.SignedAt descending
                select new ReceiptContractOption
                {
                    ContractId = contract.ContractId, 
                    Display = contract.ContractCode+" – "+seller.FullName
                }).ToList();
            }
        }
        public List<ReceiptPendingItem> AwaitingReceiptItems(int actor, int contractId)//Lấy hiện vật hợp đồng chưa nhập kho.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_RECEIPTS", "View");
                return (from d in db.ConsignmentContractDetails
                join item in db.Items on d.ItemId equals item.ItemId
                where d.ContractId == contractId && 
                !db.StockReceiptDetails.Any(rd => rd.ItemId == item.ItemId)
                orderby item.ItemCode
                select new ReceiptPendingItem
                {
                    ItemId = item.ItemId, ItemCode = item.ItemCode, 
                    ItemName = item.ItemName, ConditionAtReceipt = item.ConditionDescription, 
                    LocationAtReceipt = item.WarehouseLocation
                }).ToList();
            }
        }
        public int ConfirmStockReceipt(int actor, int contractId, IList<ReceiptLine> lines, string note, DateTime receivedAt, string deliveredBy = null)
        {//Ghi phiếu nhập, chi tiết và cập nhật hiện vật(925-1000)
            if (lines == null || lines.Count == 0 || lines.Select(x => x.ItemId).Distinct().Count() != lines.Count)
            throw new BusinessException("Phải chọn ít nhất một hiện vật, không trùng nhau.");
            if (receivedAt.Date>DateTime.Today || receivedAt.Date<new DateTime(2000, 1, 1))
            throw new BusinessException("Ngày nhận kho không hợp lệ.");
            deliveredBy = (deliveredBy??"").Trim();
            if (deliveredBy.Length<2 || deliveredBy.Length>160)
            throw new BusinessException("Cần nhập tên người giao hiện vật (2–160 ký tự).");
            if (note != null && note.Length>300)
            {
                throw new BusinessException("Ghi chú nhận kho tối đa 300 ký tự.");
            }
            foreach (var line in lines)
            {
                if (line == null || line.ItemId<=0 || string.IsNullOrWhiteSpace(line.ConditionAtReceipt) || 
                string.IsNullOrWhiteSpace(line.LocationAtReceipt) || 
                line.ConditionAtReceipt.Trim().Length>400 || line.LocationAtReceipt.Trim().Length>100)
                throw new BusinessException("Mỗi hiện vật phải ghi tình trạng (tối đa 400 ký tự) và vị trí kho (tối đa 100 ký tự).");
            }
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "STOCK_RECEIPTS", "Create");
                    var contract = db.ConsignmentContracts.SingleOrDefault(x => x.ContractId == contractId);
                    if (contract == null || contract.ContractStatus != "Active")
                    throw new BusinessException("Hợp đồng không tồn tại hoặc không còn hiệu lực.");
                    if (receivedAt.Date<contract.SignedAt.Date || 
                    (contract.ExpiresAt.HasValue && receivedAt.Date>contract.ExpiresAt.Value.Date))
                    throw new BusinessException("Ngày nhận phải nằm trong thời hạn hợp đồng ký gửi.");
                    var ids = lines.Select(x => x.ItemId).ToList();
                    var approved = db.ConsignmentContractDetails.Where(x => x.ContractId == contractId && ids.Contains(x.ItemId)).ToList();
                    if (approved.Count != ids.Count || db.StockReceiptDetails.Any(x => ids.Contains(x.ItemId)))
                    throw new BusinessException("Một hiện vật không thuộc hợp đồng hoặc đã được nhập kho.");
                    var items = db.Items.Where(x => ids.Contains(x.ItemId)).ToList();
                    if (items.Count != ids.Count || items.Any(x => x.ConsignorId != contract.ConsignorId || x.ItemStatus != "Approved"))
                    throw new BusinessException("Hiện vật không còn ở trạng thái chờ nhận hoặc sai chủ hàng.");
                    var receipt = new StockReceiptEntity
                    {
                        ReceiptCode = Code("PN"), ContractId = contractId, 
                        ReceivedAt = receivedAt, ReceivedByUserId = actor, Notes = note, DeliveredBy = deliveredBy
                    };
                    db.StockReceipts.Add(receipt);
                    db.SaveChanges();
                    foreach (var item in items)
                    {
                        var line = lines.Single(x => x.ItemId == item.ItemId);
                        string condition = line.ConditionAtReceipt.Trim(), location = line.LocationAtReceipt.Trim();
                        db.StockReceiptDetails.Add(new StockReceiptDetailEntity
                        {
                            ReceiptId = receipt.ReceiptId, 
                            ItemId = item.ItemId, ConditionAtReceipt = condition, LocationAtReceipt = location
                        });
                        item.ConditionDescription = condition;
                        item.WarehouseLocation = location;
                        item.ItemStatus = "InStock";
                    }
                    Audit(db, actor, "CONFIRM_STOCK_RECEIPT", "StockReceipts", receipt.ReceiptId, 
                    "Thủ kho xác nhận nhận "+items.Count+" hiện vật cho hợp đồng "+contract.ContractCode);
                    db.SaveChanges();
                    tx.Commit();
                    return receipt.ReceiptId;
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    RollbackSafely(tx);
                    throw new BusinessException("Dữ liệu hiện vật đã bị sửa ở máy khác.");
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Hủy hợp đồng ký gửi trước khi nhập kho: giữ lịch sử, không xóa chứng từ.
        public void CancelContractBeforeReceipt(int actor, int contractId, string reason) //Hủy hợp đồng chưa nhập kho khi đủ điều kiện(1002-1026).
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length<5 || reason.Length>250)
            throw new BusinessException("Lý do hủy hợp đồng phải từ 5 đến 250 ký tự.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "CONTRACTS", "Edit");
                    var contract = db.ConsignmentContracts.SingleOrDefault(x => x.ContractId == contractId);
                    if (contract == null || contract.ContractStatus != "Active")
                    throw new BusinessException("Chỉ hủy hợp đồng còn hiệu lực chưa nhập kho.");
                    var itemIds = db.ConsignmentContractDetails.Where(x => x.ContractId == contractId).Select(x => x.ItemId).ToList();
                    if (db.StockReceipts.Any(x => x.ContractId == contractId) || 
                    db.AuctionLots.Any(x => itemIds.Contains(x.ItemId)))
                    throw new BusinessException("Hợp đồng đã nhập kho hoặc có lot; cần quy trình điều chỉnh riêng.");
                    contract.ContractStatus = "Cancelled";
                    Audit(db, actor, "CANCEL_CONTRACT", "ConsignmentContracts", contractId, "Hủy trước nhập kho: "+reason.Trim());
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
        // Gia hạn hợp đồng hiện có; không đổi ngày ký hay mức phí đã thỏa thuận.
        public void ExtendContract(int actor, int contractId, DateTime newExpiry)//Gia hạn hợp đồng
        {
            if (newExpiry.Date<DateTime.Today)
            throw new BusinessException("Hạn hợp đồng mới không được nằm trong quá khứ.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "CONTRACTS", "Edit");
                    var contract = db.ConsignmentContracts.SingleOrDefault(x => x.ContractId == contractId);
                    if (contract == null || contract.ContractStatus != "Active")
                    throw new BusinessException("Chỉ gia hạn hợp đồng ký gửi còn ở trạng thái hoạt động.");
                    if (contract.ExpiresAt.HasValue && newExpiry.Date<=contract.ExpiresAt.Value.Date)
                    throw new BusinessException("Ngày gia hạn phải muộn hơn hạn hợp đồng cũ.");
                    contract.ExpiresAt = newExpiry.Date;
                    Audit(db, actor, "EXTEND_CONTRACT", "ConsignmentContracts", contractId, 
                    "Gia hạn hợp đồng ký gửi đến "+newExpiry.ToString("dd/MM/yyyy"));
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
        public List<ContractSummary> Contracts(int actor)//Truy vấn hợp đồng.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "CONTRACTS", "View");
                return (from c in db.ConsignmentContracts join o in db.Consignors on c.ConsignorId equals o.ConsignorId
                orderby c.SignedAt descending select new ContractSummary
                {
                    ContractId = c.ContractId, Code = c.ContractCode, Owner = o.FullName, Date = c.SignedAt, Status = c.ContractStatus, SellerFeeRate = c.SellerFeeRate, ExpiresAt = c.ExpiresAt
                }).ToList();
            }
        }
        // Chi tiết nhập kho chỉ đọc; liên kết trực tiếp với phiếu và hiện vật.
        public List<ReceiptItemRow> ReceiptItems(int actor, int receiptId)//Lấy chi tiết phiếu nhập
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_RECEIPTS", "View");
                return (from d in db.StockReceiptDetails
                join i in db.Items on d.ItemId equals i.ItemId
                where d.ReceiptId == receiptId
                orderby i.ItemCode
                select new ReceiptItemRow
                {
                    ItemId = i.ItemId, ItemCode = i.ItemCode, 
                    ItemName = i.ItemName, ItemStatus = i.ItemStatus, 
                    EstimatePrice = i.EstimatePrice, LocationAtReceipt = d.LocationAtReceipt, ConditionAtReceipt = d.ConditionAtReceipt
                }).ToList();
            }
        }
        public List<ReceiptSummary> Receipts(int actor, string keyword = "")//Truy vấn phiếu nhập.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "STOCK_RECEIPTS", "View");
                return (from r in db.StockReceipts join c in db.ConsignmentContracts on r.ContractId equals c.ContractId
                join seller in db.Consignors on c.ConsignorId equals seller.ConsignorId
                join keeper in db.Users on r.ReceivedByUserId equals keeper.UserId
                where r.ReceiptCode.Contains(keyword) || c.ContractCode.Contains(keyword) || seller.FullName.Contains(keyword)
                orderby r.ReceivedAt descending select new ReceiptSummary
                {
                    ReceiptId = r.ReceiptId, Code = r.ReceiptCode, Contract = c.ContractCode, 
                    Owner = seller.FullName, ReceivedBy = keeper.FullName, Date = r.ReceivedAt
                }).ToList();
            }
        }
    }
    public sealed class ReceiptContractOption
    {
        public int ContractId
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
    public sealed class ReceiptPendingItem
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
        public string ConditionAtReceipt
        {
            get;
            set;
        }
        public string LocationAtReceipt
        {
            get;
            set;
        }
    }
    public sealed class ReceiptLine
    {
        public int ItemId
        {
            get;
            set;
        }
        public string ConditionAtReceipt
        {
            get;
            set;
        }
        public string LocationAtReceipt
        {
            get;
            set;
        }
    }
    public sealed class ContractSummary
    {
        public int ContractId
        {
            get;
            set;
        }
        public string Code
        {
            get;
            set;
        }
        public string Owner
        {
            get;
            set;
        }
        public DateTime Date
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
    public sealed class ReceiptItemRow
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
        public string ItemStatus
        {
            get;
            set;
        }
        public decimal EstimatePrice
        {
            get;
            set;
        }
        public string LocationAtReceipt
        {
            get;
            set;
        }
        public string ConditionAtReceipt
        {
            get;
            set;
        }
    }
    public sealed class ReceiptSummary
    {
        public int ReceiptId
        {
            get;
            set;
        }
        public string Code
        {
            get;
            set;
        }
        public string Contract
        {
            get;
            set;
        }
        public string Owner
        {
            get;
            set;
        }
        public string ReceivedBy
        {
            get;
            set;
        }
        public DateTime Date
        {
            get;
            set;
        }
    }
}
