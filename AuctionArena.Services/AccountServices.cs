using System;
using System.Data.Entity;
using System.Linq;
using System.Collections.Generic;
using AuctionArena.Data;
using AuctionArena.Data.Models;
using AuctionArena.Data.Security;
namespace AuctionArena.Services
{
    // Kiểm tra thông tin tài khoản trước khi cập nhật quyền hoặc mật khẩu.
    // Mật khẩu được kiểm tra qua PasswordHasher, không so sánh chuỗi gốc.
    public sealed class AccountService : ServiceBase
    {
        public AccountService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
            // Khởi tạo AccountService. Nhận phụ thuộc/cấu hình ban đầu.
        }
        public UserInfo Login(string username, string password)
        {
            // Kiểm tra tài khoản, hash, khóa tạm và cập nhật lịch sử đăng  nhập ( 17-75).
          
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                bool committed = false;
                try
                {
                    var user = db.Users.SingleOrDefault(x => x.Username == (username??"").Trim() && x.IsActive);
                    if (user == null)
                    {
                        throw new BusinessException("Tài khoản hoặc mật khẩu không đúng.");
                    }
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value>DateTime.UtcNow)
                    throw new BusinessException("Tài khoản tạm khóa do nhập sai mật khẩu. Hãy thử lại sau.");
                    bool ok = false;
                    try
                    {
                        ok = PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash, user.HashIterations);
                    }
                    catch (ArgumentException)
                    {
                        ok = false;
                    }
                    if (!ok)
                    {
                        user.FailedPasswordAttempts = Math.Min(5, user.FailedPasswordAttempts+1);
                        if (user.FailedPasswordAttempts>=5)
                        {
                            user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                            Audit(db, user.UserId, "LOGIN_TEMP_LOCK", "Users", user.UserId, "Tạm khóa 15 phút sau 5 lần nhập sai");
                        }
                        db.SaveChanges();
                        tx.Commit();
                        committed = true;
                        throw new BusinessException("Tài khoản hoặc mật khẩu không đúng.");
                    }
                    if (!db.Roles.Any(x => x.RoleId == user.RoleId && x.IsActive))
                    throw new BusinessException("Nhóm quyền của tài khoản đã bị khóa.");
                    user.FailedPasswordAttempts = 0;
                    user.LockedUntil = null;
                    user.LastLoginAt = DateTime.UtcNow;
                    Audit(db, user.UserId, "LOGIN", "Users", user.UserId, "Đăng nhập thành công");
                    db.SaveChanges();
                    tx.Commit();
                    committed = true;
                    return new UserInfo
                    {
                        UserId = user.UserId, RoleId = user.RoleId, FullName = user.FullName, 
                        MustChangePassword = user.MustChangePassword
                    };
                }
                catch
                {
                    if (!committed)RollbackSafely(tx);
                    throw;
                }
            }
        }
        public string OwnRoleName(int actor)
        {
            // Lấy tên nhóm của người đang thao tác ( 79-89).
            using (var db = Factory())
            return (from u in db.Users join r in db.Roles on u.RoleId equals r.RoleId
            where u.UserId == actor && u.IsActive select r.RoleName).Single();
        }
        public string OwnUsername(int actor)
        { // Lấy tên đăng nhập của chính người dùng (86-89).
            using (var db = Factory())
            return db.Users.Where(x => x.UserId == actor && x.IsActive).Select(x => x.Username).Single();
        }
        public void ChangePassword(int actor, string oldPassword, string newPassword)
        { // Xác minh mật khẩu cũ và lưu hash/salt mới ( 91-131 )
            if (!StrongPassword(newPassword))
            throw new BusinessException("Mật khẩu mới cần ít nhất 8 ký tự, chữ hoa, chữ thường và số.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                bool committed = false;
                try
                {
                    var user = db.Users.Single(x => x.UserId == actor && x.IsActive);
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value>DateTime.UtcNow)
                    throw new BusinessException("Tài khoản đã tạm khóa. Hãy thử lại sau.");
                    bool match = PasswordHasher.Verify(oldPassword, user.PasswordSalt, user.PasswordHash, user.HashIterations);
                    if (!match)
                    {
                        user.FailedPasswordAttempts = Math.Min(5, user.FailedPasswordAttempts+1);
                        if (user.FailedPasswordAttempts>=5)
                        {
                            user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                            Audit(db, actor, "PASSWORD_TEMP_LOCK", "Users", actor, "Tạm khóa 15 phút do sai mật khẩu hiện tại");
                        }
                        db.SaveChanges();
                        tx.Commit();
                        committed = true;
                        throw new BusinessException("Mật khẩu hiện tại không đúng.");
                    }
                    if (PasswordHasher.Verify(newPassword, user.PasswordSalt, user.PasswordHash, user.HashIterations))
                    throw new BusinessException("Mật khẩu mới phải khác mật khẩu hiện tại.");
                    user.PasswordSalt = PasswordHasher.CreateSalt();
                    user.HashIterations = PasswordHasher.DefaultIterations;
                    user.PasswordHash = PasswordHasher.Hash(newPassword, user.PasswordSalt, user.HashIterations);
                    user.PasswordChangedAt = DateTime.UtcNow;
                    user.MustChangePassword = false;
                    user.FailedPasswordAttempts = 0;
                    user.LockedUntil = null;
                    Audit(db, actor, "PASSWORD_CHANGE", "Users", actor, "Đổi mật khẩu của chính mình");
                    db.SaveChanges();
                    tx.Commit();
                    committed = true;
                }
                catch
                {
                    if (!committed)RollbackSafely(tx);
                    throw;
                }
            }
        }
        public static bool StrongPassword(string value)
        { // Kiểm tra quy tắc độ mạnh mật khẩu
            return value != null && value.Length>=8 && value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit);
        }
        public void Logout(int actor)
        { // Ghi nhật ký đăng xuất
            using (var db = Factory())
            {
                Audit(db, actor, "LOGOUT", "Users", actor, "Đăng xuất khỏi hệ thống");
                db.SaveChanges();
            }
        }
        public Dictionary<string, PermissionFlags> Permissions(int actor)
        { //Lấy quyền chức năng theo nhóm người dùng.
            using (var db = Factory())
            {
                var user = db.Users.SingleOrDefault(x => x.UserId == actor && x.IsActive);
                if (user == null || !db.Roles.Any(r => r.RoleId == user.RoleId && r.IsActive))
                throw new BusinessException("Phiên đăng nhập không hợp lệ.");
                return (from rp in db.RolePermissions join p in db.Permissions on rp.PermissionId equals p.PermissionId
                where rp.RoleId == user.RoleId
                select new
                {
                    p.PermissionCode, rp.CanView, rp.CanCreate, rp.CanEdit, rp.CanDelete, rp.CanPrint
                })
                .ToList().ToDictionary(x => x.PermissionCode, x => new PermissionFlags
                {
                    View = x.CanView, Create = x.CanCreate, Edit = x.CanEdit, Delete = x.CanDelete, Print = x.CanPrint
                });
            }
        }
    }
    public sealed class UserInfo
    {
        public int UserId
        {
            get;
            set;
        }
        public int RoleId
        {
            get;
            set;
        }
        public string FullName
        {
            get;
            set;
        }
        public bool MustChangePassword
        {
            get;
            set;
        }
    }
    public sealed class PermissionFlags
    {
        public bool View
        {
            get;
            set;
        }
        public bool Create
        {
            get;
            set;
        }
        public bool Edit
        {
            get;
            set;
        }
        public bool Delete
        {
            get;
            set;
        }
        public bool Print
        {
            get;
            set;
        }
    }
    public sealed class AdministrationService : ServiceBase
    {//Khởi tạo AdministrationService. Nhận phụ thuộc/cấu hình ban đầu.
        public AdministrationService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        public List<UserRow> GetUsers(int actor, string keyword = "", int? roleId = null, bool? active = null) // Truy vấn danh sách người dùng.
        {
            keyword = (keyword??"").Trim();
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "USERS", "View");
                return (from u in db.Users join r in db.Roles on u.RoleId equals r.RoleId
                where (u.Username.Contains(keyword) || u.FullName.Contains(keyword) || u.Email.Contains(keyword)) && 
                (!roleId.HasValue || u.RoleId == roleId.Value) && (!active.HasValue || u.IsActive == active.Value)
                orderby u.Username
                select new UserRow
                {
                    UserId = u.UserId, Username = u.Username, FullName = u.FullName, 
                    RoleId = u.RoleId, RoleName = r.RoleName, Email = u.Email, Phone = u.Phone, IsActive = u.IsActive, 
                    CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt, MustChangePassword = u.MustChangePassword
                }).ToList();
            }
        }
        public List<UserAuditRow> UserAudit(int actor, int? targetId, string action, DateTime from, DateTime to) //Lấy lịch sử liên quan tài khoản.
        {
            if (from.Date>to.Date || (to.Date-from.Date).TotalDays>366)
            throw new BusinessException("Khoảng thời gian nhật ký không hợp lệ hoặc quá 366 ngày.");
            action = (action??"").Trim();
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "USERS", "View");
                var f = from.Date;
                var end = to.Date.AddDays(1);
                return db.AuditLogs.Where(x => x.OccurredAt>=f && x.OccurredAt<end && 
                (!targetId.HasValue || x.UserId == targetId.Value) && x.ActionCode.Contains(action))
                .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.AuditLogId)
                .Take(500).Select(x => new UserAuditRow
                {
                    AuditLogId = x.AuditLogId, UserId = x.UserId, 
                    ActionCode = x.ActionCode, OccurredAt = x.OccurredAt, Description = x.Description
                }).ToList();
            }
        }
        public List<RoleOption> GetRoles(int actor, bool forUserManagement = false) // Lấy nhóm quyền
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, forUserManagement ? "USERS" : "ROLES", "View");
                return db.Roles.OrderBy(r => r.RoleName).Select(r => new RoleOption
                {
                    Id = r.RoleId, Name = r.RoleName, Code = r.RoleCode, Active = r.IsActive, Description = r.Description
                }).ToList();
            }
        }
        public void SaveUser(int actor, int id, string username, string fullname, int role, bool active, string password, string email = null, string phone = null, string reason = null)
        { // kiểm tra và lưu tài khoản ( 275-389 )
            username = (username ?? "").Trim();
            fullname = (fullname ?? "").Trim();
            if (username.Length < 3 || fullname.Length < 2)
            {
                throw new BusinessException("Vui lòng nhập tên đăng nhập và họ tên hợp lệ.");
            }
            email = (email??"").Trim();
            phone = (phone??"").Trim();
            reason = (reason??"").Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,60}$"))
            throw new BusinessException("Tên đăng nhập 3–60 ký tự, không dấu và không có khoảng trắng.");
            if (email.Length>0 && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            throw new BusinessException("Email không đúng định dạng.");
            if (phone.Length>0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[0-9]{9}$"))
            throw new BusinessException("Số điện thoại cần 10 chữ số, bắt đầu bằng 0.");
            if (id == 0 && !AccountService.StrongPassword(password))
            throw new BusinessException("Mật khẩu ban đầu cần ≥8 ký tự, chữ hoa, chữ thường, số.");
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "USERS", id == 0 ? "Create" : "Edit");
                    if (!db.Roles.Any(r => r.RoleId == role && r.IsActive))
                    {
                        throw new BusinessException("Nhóm quyền không hợp lệ.");
                    }
                    bool targetAdmin = db.Roles.Any(r => r.RoleId == role && r.RoleCode == "ADMIN");
                    bool actorAdmin = (from au in db.Users join ar in db.Roles on au.RoleId equals ar.RoleId
                    where au.UserId == actor && ar.RoleCode == "ADMIN" select au.UserId).Any();
                    if (targetAdmin && !actorAdmin)
                    {
                        throw new BusinessException("Chỉ quản trị viên mới được gán nhóm Admin.");
                    }
                    if (db.Users.Any(x => x.Username == username && x.UserId != id))
                    {
                        throw new BusinessException("Tên đăng nhập đã tồn tại.");
                    }
                    var u = id == 0 ? new UserEntity
                    {
                        CreatedAt = DateTime.UtcNow
                    }
                    : db.Users.Single(x => x.UserId == id);
                    if (id == actor && !active)
                    {
                        throw new BusinessException("Bạn không thể tự khóa tài khoản đang đăng nhập.");
                    }
                    if (id == actor && role != u.RoleId && id != 0)
                    {
                        throw new BusinessException("Không thể tự đổi nhóm quyền của mình.");
                    }
                    if (id != 0 && u.Username != username)
                    {
                        throw new BusinessException("Không được đổi tên đăng nhập sau khi tạo.");
                    }
                    bool previousAdmin = id != 0 && db.Roles.Any(r => r.RoleId == u.RoleId && r.RoleCode == "ADMIN");
                    if (previousAdmin && u.IsActive && (!active || !targetAdmin) && 
                    (from u0 in db.Users join r0 in db.Roles on u0.RoleId equals r0.RoleId
                    where u0.IsActive && r0.RoleCode == "ADMIN" select u0.UserId).Count()<=1)
                    throw new BusinessException("Phải giữ ít nhất một Admin đang hoạt động.");
                    if (id != 0 && u.IsActive && !active && reason.Length<5)
                    throw new BusinessException("Cần ghi lý do khóa tài khoản từ 5 ký tự.");
                    string previousStatus = id == 0?"(mới)":(u.IsActive?"Hoạt động":"Khóa");
                    int previousRole = id == 0?0:u.RoleId;
                    u.Username = username;
                    u.FullName = fullname;
                    u.RoleId = role;
                    u.IsActive = active;
                    u.Email = email;
                    u.Phone = phone;
                    u.LockReason = active?null:reason;
                    if (!string.IsNullOrEmpty(password))
                    {
                        if (!AccountService.StrongPassword(password))
                        throw new BusinessException("Mật khẩu cần ≥8 ký tự, chữ hoa, chữ thường và số.");
                        u.PasswordSalt = PasswordHasher.CreateSalt();
                        u.HashIterations = PasswordHasher.DefaultIterations;
                        u.PasswordHash = PasswordHasher.Hash(password, u.PasswordSalt, u.HashIterations);
                        u.MustChangePassword = id == 0;
                        u.PasswordChangedAt = DateTime.UtcNow;
                    }
                    if (id == 0) db.Users.Add(u);
                    db.SaveChanges();
                    Audit(db, actor, "USER_SAVE", "Users", u.UserId, 
                    "Lưu tài khoản "+username+" | Nhóm "+previousRole+" → "+u.RoleId+
                    " | Trạng thái "+previousStatus+" → "+(u.IsActive?"Hoạt động":"Khóa"));
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
        public string ResetPassword(int actor, int targetId) //Đặt lại mật khẩu và yêu cầu đổi khi đăng nhập (373-414)
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "USERS", "Edit");
                    if (actor == targetId)
                    {
                        throw new BusinessException("Hãy dùng chức năng Đổi mật khẩu cho chính mình.");
                    }
                    var admin = (from a in db.Users join r in db.Roles on a.RoleId equals r.RoleId
                    where a.UserId == actor && r.RoleCode == "ADMIN" select a.UserId).Any();
                    if (!admin)
                    {
                        throw new BusinessException("Chỉ Admin mới được đặt lại mật khẩu của người khác.");
                    }
                    var u = db.Users.SingleOrDefault(x => x.UserId == targetId);
                    if (u == null)
                    {
                        throw new BusinessException("Không tìm thấy người dùng.");
                    }
                    var chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$";
                    var bytes = new byte[18];
                    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())rng.GetBytes(bytes);
                    var temporary = "Aa7!"+new string(bytes.Select(x => chars[x%chars.Length]).ToArray());
                    u.PasswordSalt = PasswordHasher.CreateSalt();
                    u.HashIterations = PasswordHasher.DefaultIterations;
                    u.PasswordHash = PasswordHasher.Hash(temporary, u.PasswordSalt, u.HashIterations);
                    u.MustChangePassword = true;
                    u.PasswordChangedAt = DateTime.UtcNow;
                    u.FailedPasswordAttempts = 0;
                    u.LockedUntil = null;
                    Audit(db, actor, "USER_PASSWORD_RESET", "Users", targetId, "Đặt lại mật khẩu tạm; bắt buộc đổi lần đăng nhập tới");
                    db.SaveChanges();
                    tx.Commit();
                    return temporary;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public List<RoleMemberRow> GetRoleMembers(int actor, int roleId) // Lấy thành viên nhóm.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ROLES", "View");
                return db.Users.Where(x => x.RoleId == roleId).OrderBy(x => x.Username)
                .Select(x => new RoleMemberRow
                {
                    UserId = x.UserId, Username = x.Username, 
                    FullName = x.FullName, IsActive = x.IsActive
                }).ToList();
            }
        }
        public void DeleteRole(int actor, int id) // Xóa nhóm khi thỏa điều kiện(431-457).
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "ROLES", "Delete");
                    var r = db.Roles.SingleOrDefault(x => x.RoleId == id);
                    if (r == null)
                    {
                        throw new BusinessException("Không tìm thấy nhóm quyền.");
                    }
                    if (r.RoleCode == "ADMIN")
                    {
                        throw new BusinessException("Không thể xóa nhóm Admin.");
                    }
                    if (db.Users.Any(x => x.RoleId == id))throw new BusinessException("Nhóm có thành viên; hãy chuyển nhóm trước.");
                    db.RolePermissions.RemoveRange(db.RolePermissions.Where(x => x.RoleId == id));
                    db.Roles.Remove(r);
                    Audit(db, actor, "ROLE_DELETE", "Roles", id, "Xóa nhóm trống "+r.RoleCode);
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
        public int CloneRole(int actor, int sourceId, string code, string name) //Sao chép nhóm và quyền ( 461 - 505 ).
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "ROLES", "Create");
                    var src = db.Roles.SingleOrDefault(x => x.RoleId == sourceId);
                    if (src == null)
                    {
                        throw new BusinessException("Không tìm thấy nhóm quyền nguồn.");
                    }
                    if (src.RoleCode == "ADMIN")
                    {
                        throw new BusinessException("Không cho phép nhân bản toàn bộ quyền của Admin.");
                    }
                    code = (code??"").Trim().ToUpperInvariant();
                    name = (name??"").Trim();
                    if (code.Length<2 || name.Length<2 || db.Roles.Any(x => x.RoleCode == code))
                    throw new BusinessException("Tên hoặc mã nhóm nhân bản không hợp lệ/bị trùng.");
                    var r = new RoleEntity
                    {
                        RoleCode = code, RoleName = name, Description = src.Description, IsActive = true
                    };
                    db.Roles.Add(r);
                    db.SaveChanges();
                    foreach (var old in db.RolePermissions.Where(x => x.RoleId == sourceId).ToList())
                    db.RolePermissions.Add(new RolePermissionEntity
                    {
                        RoleId = r.RoleId, PermissionId = old.PermissionId, 
                        CanView = old.CanView, CanCreate = old.CanCreate, CanEdit = old.CanEdit, 
                        CanDelete = old.CanDelete, CanPrint = old.CanPrint
                    });
                    Audit(db, actor, "ROLE_CLONE", "Roles", r.RoleId, "Nhân bản nhóm "+src.RoleCode);
                    db.SaveChanges();
                    tx.Commit();
                    return r.RoleId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Tài khoản đã phát sinh chứng từ sẽ bị khóa thay vì xóa.
        // Giá trị trả về: true khi xóa được, false khi chỉ khóa tài khoản.
        public bool DeleteOrLockUser(int actor, int userId, string reason) // Xóa hoặc khóa tài khoản tùy ràng buộc/lịch sử ( 508-564).
        {
            reason = (reason??"").Trim();
            if (reason.Length<5 || reason.Length>250)
            throw new BusinessException("Nhập lý do từ 5 đến 250 ký tự.");
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "USERS", "Delete");
                    if (actor == userId)
                    {
                        throw new BusinessException("Không thể xóa hoặc khóa tài khoản đang đăng nhập.");
                    }
                    var account = db.Users.SingleOrDefault(x => x.UserId == userId);
                    if (account == null)
                    {
                        throw new BusinessException("Tài khoản không còn tồn tại.");
                    }
                    bool targetAdmin = db.Roles.Any(x => x.RoleId == account.RoleId && x.RoleCode == "ADMIN");
                    if (targetAdmin && account.IsActive && 
                    (from u in db.Users join role in db.Roles on u.RoleId equals role.RoleId
                    where u.IsActive && role.RoleCode == "ADMIN" select u.UserId).Count()<=1)
                    throw new BusinessException("Không được xóa/khóa quản trị viên hoạt động cuối cùng.");
                    bool hasHistory = db.AuditLogs.Any(x => x.UserId == userId) || 
                    db.StockReceipts.Any(x => x.ReceivedByUserId == userId) || 
                    db.StockIssues.Any(x => x.IssuedByUserId == userId) || 
                    db.ConsignorReturns.Any(x => x.ReturnedByUserId == userId) || 
                    db.AuctionSessions.Any(x => x.ApprovedByUserId == userId) || 
                    db.Bids.Any(x => x.EnteredByUserId == userId) || 
                    db.Payments.Any(x => x.ReceivedByUserId == userId) || 
                    db.SellerPayouts.Any(x => x.PaidByUserId == userId) || 
                    db.SellerPayoutOrders.Any(x => x.CreatedByUserId == userId) || 
                    db.InvoiceReviewCases.Any(x => x.CreatedByUserId == userId) || 
                    db.InvoiceAdjustments.Any(x => x.CreatedByUserId == userId);
                    if (hasHistory)
                    {
                        account.IsActive = false;
                        Audit(db, actor, "USER_LOCK_INSTEAD_DELETE", "Users", userId, 
                        "Khóa tài khoản có lịch sử nghiệp vụ. Lý do: "+reason);
                    }
                    else
                    {
                        var loginName = account.Username;
                        db.Users.Remove(account);
                        Audit(db, actor, "USER_DELETE", "Users", userId, 
                        "Xóa tài khoản chưa phát sinh nghiệp vụ: "+loginName+". Lý do: "+reason);
                    }
                    db.SaveChanges();
                    tx.Commit();
                    return !hasHistory;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        // Không xóa nhóm còn thành viên và không cho sửa nhóm Admin.
        public int SaveRole(int actor, int id, string code, string name, bool active, string description = null) // Lưu thông tin nhóm ( 569-623 ).
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            name = (name ?? "").Trim();
            description = (description??"").Trim();
            if (description.Length>300)
            { 
                throw new BusinessException("Mô tả nhóm không quá 300 ký tự.");
            }
            if (code.Length < 2 || code.Length > 40 || name.Length < 2 || name.Length > 100 || 
            code.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
            throw new BusinessException("Mã nhóm dùng chữ, số hoặc dấu gạch dưới; tên nhóm phải từ 2 ký tự.");
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "ROLES", id == 0 ? "Create" : "Edit");
                    if (db.Roles.Any(r => r.RoleCode == code && r.RoleId != id))
                    throw new BusinessException("Mã nhóm quyền đã tồn tại.");
                    RoleEntity role;
                    if (id == 0)
                    { 
                        role = new RoleEntity(); 
                        role = new RoleEntity();
                        db.Roles.Add(role);
                    }
                    else
                    {
                        role = db.Roles.SingleOrDefault(r => r.RoleId == id);
                        if (role == null)
                        {
                            throw new BusinessException("Không tìm thấy nhóm quyền.");
                        }
                        var current = db.Users.Single(u => u.UserId == actor);
                        if (role.RoleCode == "ADMIN" || current.RoleId == id)
                        throw new BusinessException("Không sửa nhóm Admin hoặc nhóm quyền của chính mình.");
                        if (!active && db.Users.Any(u => u.RoleId == id && u.IsActive))
                        throw new BusinessException("Còn tài khoản đang hoạt động trong nhóm; hãy chuyển nhóm trước.");
                    }
                    var before = id == 0?"(mới)":role.RoleCode+"/"+role.RoleName+"/"+role.IsActive;
                    role.RoleCode = code;
                    role.RoleName = name;
                    role.Description = description;
                    role.IsActive = active;
                    db.SaveChanges();
                    Audit(db, actor, "ROLE_SAVE", "Roles", role.RoleId, before+" → "+
                    role.RoleCode+"/"+role.RoleName+"/"+role.IsActive);
                    db.SaveChanges();
                    tx.Commit();
                    return role.RoleId;
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public List<RolePermissionRow> GetRolePermissions(int actor, int role) // Lấy ma trận quyền của nhóm.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ROLES", "View");
                return (from p in db.Permissions
                join rp in db.RolePermissions.Where(x => x.RoleId == role) on p.PermissionId equals rp.PermissionId into grouped
                from rp in grouped.DefaultIfEmpty()
                orderby p.PermissionName
                select new RolePermissionRow
                {
                    PermissionId = p.PermissionId, Name = p.PermissionName, 
                    CanView = rp != null && rp.CanView, CanCreate = rp != null && rp.CanCreate, 
                    CanEdit = rp != null && rp.CanEdit, CanDelete = rp != null && rp.CanDelete, 
                    CanPrint = rp != null && rp.CanPrint
                }).ToList();
            }
        }
        public void SaveRolePermissions(int actor, int role, List<RolePermissionRow> rows) // Kiểm tra và lưu các cờ quyền ( 646 - 703 )
        {
            if (rows == null || rows.Select(x => x.PermissionId).Distinct().Count() != rows.Count)
            throw new BusinessException("Dữ liệu quyền không hợp lệ.");
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "ROLES", "Edit");
                    var target = db.Roles.Single(x => x.RoleId == role);
                    var current = db.Users.Single(x => x.UserId == actor);
                    if (target.RoleCode == "ADMIN" || current.RoleId == role)
                    throw new BusinessException("Không sửa quyền Admin hoặc nhóm quyền của chính mình.");
                    var ids = db.Permissions.Select(x => x.PermissionId).ToList();
                    if (rows.Count != ids.Count || rows.Any(x => !ids.Contains(x.PermissionId)))
                    throw new BusinessException("Danh sách quyền không khớp cơ sở dữ liệu.");
                    var existing = db.RolePermissions.Where(x => x.RoleId == role).ToList();
                    var before = existing.ToDictionary(x => x.PermissionId, x => 
                    ""+(x.CanView?'1':'0')+(x.CanCreate?'1':'0')+(x.CanEdit?'1':'0')+
                    (x.CanDelete?'1':'0')+(x.CanPrint?'1':'0'));
                    foreach (var row in rows)
                    {
                        var rp = existing.SingleOrDefault(x => x.PermissionId == row.PermissionId);
                        if (rp == null)
                        {
                            rp = new RolePermissionEntity
                            {
                                RoleId = role, PermissionId = row.PermissionId
                            };
                            db.RolePermissions.Add(rp);
                        }
                        rp.CanView = row.CanView || row.CanCreate || row.CanEdit || row.CanDelete || row.CanPrint;
                        rp.CanCreate = row.CanCreate;
                        rp.CanEdit = row.CanEdit;
                        rp.CanDelete = row.CanDelete;
                        rp.CanPrint = row.CanPrint;
                    }
                    var deltas = rows.Select(x => 
                    {
                        string previous;
                        if (!before.TryGetValue(x.PermissionId, out previous))previous = "00000";
                        // Lưu trạng thái Xem, Thêm, Sửa, Xóa và In của nhóm.
                        string newPermissions = "" + ((x.CanView || x.CanCreate || x.CanEdit || x.CanDelete || x.CanPrint) ? '1' : '0') +
                            (x.CanCreate ? '1' : '0') + (x.CanEdit ? '1' : '0') +
                            (x.CanDelete ? '1' : '0') + (x.CanPrint ? '1' : '0');
                        return previous == newPermissions ? null : x.PermissionId + ":" + previous + ">" + newPermissions;
                    }).Where(x => x != null).ToList();
                    var auditText = "Ma trận quyền X/T/S/X/I: "+string.Join(";", deltas);
                    if (auditText.Length>980)auditText = auditText.Substring(0, 980);
                    Audit(db, actor, "ROLE_PERMISSIONS", "Roles", role, auditText);
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
    public sealed class RoleOption
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
        public string Code
        {
            get;
            set;
        }
        public bool Active
        {
            get;
            set;
        }
        public string Description
        {
            get;
            set;
        }
        public override string ToString() // Cung cấp nhãn hiển thị cho đối tượng lựa chọn
        {
            return Name;
        }
    }
    public sealed class UserRow
    {
        public int UserId
        {
            get;
            set;
        }
        public string Username
        {
            get;
            set;
        }
        public string FullName
        {
            get;
            set;
        }
        public int RoleId
        {
            get;
            set;
        }
        public string RoleName
        {
            get;
            set;
        }
        public string Email
        {
            get;
            set;
        }
        public string Phone
        {
            get;
            set;
        }
        public bool IsActive
        {
            get;
            set;
        }
        public DateTime CreatedAt
        {
            get;
            set;
        }
        public DateTime? LastLoginAt
        {
            get;
            set;
        }
        public bool MustChangePassword
        {
            get;
            set;
        }
    }
    public sealed class UserAuditRow
    {
        public long AuditLogId
        {
            get;
            set;
        }
        public int? UserId
        {
            get;
            set;
        }
        public DateTime OccurredAt
        {
            get;
            set;
        }
        public string ActionCode
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
    public sealed class RoleMemberRow
    {
        public int UserId
        {
            get;
            set;
        }
        public string Username
        {
            get;
            set;
        }
        public string FullName
        {
            get;
            set;
        }
        public bool IsActive
        {
            get;
            set;
        }
    }
    public sealed class RolePermissionRow
    {
        public int PermissionId
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public bool CanView
        {
            get;
            set;
        }
        public bool CanCreate
        {
            get;
            set;
        }
        public bool CanEdit
        {
            get;
            set;
        }
        public bool CanDelete
        {
            get;
            set;
        }
        public bool CanPrint
        {
            get;
            set;
        }
    }
    public sealed class DashboardPeriodSummary
    {
        public int OpenSessions
        {
            get;
            set;
        }
        public int UpcomingSessions
        {
            get;
            set;
        }
        public int InStock
        {
            get;
            set;
        }
        public int SoldLots
        {
            get;
            set;
        }
        public int UnpaidInvoices
        {
            get;
            set;
        }
        public decimal UnpaidAmount
        {
            get;
            set;
        }
        public int NewCustomers
        {
            get;
            set;
        }
        public decimal PendingSellerPayout
        {
            get;
            set;
        }
        public decimal Revenue
        {
            get;
            set;
        }
        public decimal PreviousRevenue
        {
            get;
            set;
        }
    }
    public sealed class DashboardUpcoming
    {
        public int SessionId
        {
            get;
            set;
        }
        public string SessionCode
        {
            get;
            set;
        }
        public string SessionName
        {
            get;
            set;
        }
        public DateTime StartsAt
        {
            get;
            set;
        }
    }
    public sealed class DashboardTask
    {
        public string Group
        {
            get;
            set;
        }
        public int Count
        {
            get;
            set;
        }
    }
    public sealed class DashboardPeriodService : ServiceBase 
    {
        public DashboardPeriodService(Func<AuctionArenaDbContext> factory = null):base(factory)//Khởi tạo DashboardPeriodService. Nhận phụ thuộc/cấu hình banđầu.
        {
        }
        public DashboardPeriodSummary Load(int actor, DateTime from, DateTime to) // Tổng hợp dữ liệu theo điều kiện đầu vào ( 981 - 1030 )
        {
            if (from>=to)
            {
                throw new BusinessException("Khoảng kỳ xem không hợp lệ.");
            }
            using (var db = Factory())
            {
                var role = (from u in db.Users join r in db.Roles on u.RoleId equals r.RoleId
                where u.UserId == actor && u.IsActive && r.IsActive select r.RoleId).SingleOrDefault();
                if (role<=0)
                {
                    throw new BusinessException("Phiên đăng nhập không hợp lệ.");
                }
                Func<string, bool> allowed = key => (from rp in db.RolePermissions join per in db.Permissions
                on rp.PermissionId equals per.PermissionId where rp.RoleId == role && 
                per.PermissionCode == key && rp.CanView select rp.RoleId).Any();
                var now = DateTime.Now;
                var result = new DashboardPeriodSummary();
                if (allowed("SESSIONS"))
                {
                    result.OpenSessions = db.AuctionSessions.Count(x => x.SessionStatus == "Open");
                    result.UpcomingSessions = db.AuctionSessions.Count(x => x.SessionStatus == "Planned" && x.StartsAt>=now);
                }
                if (allowed("ITEMS"))result.InStock = db.Items.Count(x => x.ItemStatus == "InStock");
                if (allowed("LOTS"))result.SoldLots = db.AuctionLots.Count(x => x.LotStatus == "Sold" && x.ClosesAt>=from && x.ClosesAt<to);
                if (allowed("CUSTOMERS"))result.NewCustomers = db.Customers.Count(x => x.CreatedAt>=from && x.CreatedAt<to);
                if (allowed("INVOICES"))
                {
                    var unpaid = db.Invoices.Where(x => x.InvoiceStatus != "Cancelled" && x.InvoiceStatus != "Paid" && x.IssuedAt>=from && x.IssuedAt<to);
                    result.UnpaidInvoices = unpaid.Count();
                    result.UnpaidAmount = unpaid.Sum(x => (decimal?)(x.TotalDue-x.PaidAmount))??0m;
                }
                if (allowed("PAYOUTS"))result.PendingSellerPayout = db.SellerPayoutOrders.Where(x => x.OrderStatus == "Pending")
                .Sum(x => (decimal?)x.PayoutAmount)??0m;
                if (allowed("REPORTS"))
                {
                    var previous = from-(to-from);
                    // Doanh thu được ghi theo ngày thanh toán cuối, không lấy ngày lập hóa đơn.
                    var invoices = (from i in db.Invoices where i.InvoiceStatus == "Paid"
                    select new
                    {
                        Amount = i.BuyerFee-i.VipDiscount, 
                        PaidAt = db.Payments.Where(p => p.InvoiceId == i.InvoiceId && p.PaymentType == "Invoice")
                        .Max(p => (DateTime?)p.PaidAt)
                    }).ToList();
                    result.Revenue = invoices.Where(x => x.PaidAt>=from && x.PaidAt<to).Sum(x => x.Amount);
                    result.PreviousRevenue = invoices.Where(x => x.PaidAt>=previous && x.PaidAt<from).Sum(x => x.Amount);
                }
                return result;
            }
        }
        public List<DashboardUpcoming> Upcoming(int actor) // Lấy phiên sắp diễn ra.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "SESSIONS", "View");
                var now = DateTime.Now;
                return db.AuctionSessions.Where(x => x.SessionStatus == "Planned" && x.StartsAt>=now)
                .OrderBy(x => x.StartsAt).Take(5).Select(x => new DashboardUpcoming
                {
                    SessionId = x.SessionId, SessionCode = x.SessionCode, SessionName = x.SessionName, StartsAt = x.StartsAt
                }).ToList();
            }
        }
        public List<DashboardTask> Tasks(int actor) // Lấy phiên sắp diễn ra.
        {
            using (var db = Factory())
            {
                var user = db.Users.SingleOrDefault(x => x.UserId == actor && x.IsActive);
                if (user == null)
                {
                    throw new BusinessException("Phiên đăng nhập không hợp lệ.");
                }
                var flags = (from rp in db.RolePermissions join permission in db.Permissions
                on rp.PermissionId equals permission.PermissionId
                where rp.RoleId == user.RoleId select new
                {
                    permission.PermissionCode, rp.CanView
                })
                .ToList().ToDictionary(x => x.PermissionCode, x => new PermissionFlags
                {
                    View = x.CanView
                });
                var tasks = new List<DashboardTask>();
                if (flags.ContainsKey("ITEMS") && flags["ITEMS"].View)
                tasks.Add(new DashboardTask
                {
                    Group = "Hiện vật chờ thẩm định", Count = db.Items.Count(x => x.ItemStatus == "PendingAppraisal")
                });
                if (flags.ContainsKey("INVOICES") && flags["INVOICES"].View)
                tasks.Add(new DashboardTask
                {
                    Group = "Hóa đơn quá hạn", Count = db.Invoices.Count(x => x.InvoiceStatus != "Paid" && x.InvoiceStatus != "Cancelled" && x.DueAt<DateTime.Now)
                });
                if (flags.ContainsKey("PAYOUTS") && flags["PAYOUTS"].View)
                tasks.Add(new DashboardTask
                {
                    Group = "Lệnh chi chủ hàng chờ xử lý", Count = db.SellerPayoutOrders.Count(x => x.OrderStatus == "Pending")
                });
                return tasks;
            }
        }
    }
}
