using System;
using System.Data;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
using AuctionArena.Data.Security;
namespace AuctionArena.Services
{
    // Chỉ dùng cho database mới chưa có bất kỳ tài khoản nào; không can thiệp DB hiện tại.
    public sealed class InitialAdminService : ServiceBase
    {
        public InitialAdminService(Func<AuctionArenaDbContext> factory = null):base(factory)
        {
        }
        public bool NeedsInitialAdmin() //Kiểm tra database chưa có người dùng.
        {
            using (var db = Factory())return !db.Users.Any();
        }
        public void Create(string username, string fullname, string password) //Tạo Admin đầu tiên với hash/salt và nhật ký
        {
            username = (username??"").Trim();
            fullname = (fullname??"").Trim();
            if (username.Length<3 || username.Length>60 || 
            !System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
            throw new BusinessException("Tên đăng nhập phải có 3–60 ký tự chữ, số, dấu chấm hoặc gạch nối.");
            if (fullname.Length<3 || fullname.Length>120)
            throw new BusinessException("Tên quản trị phải có 3–120 ký tự.");
            if (!AccountService.StrongPassword(password) || password.Length>128)
            throw new BusinessException("Mật khẩu quản trị cần 8–128 ký tự, chữ hoa, chữ thường và số.");
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    if (db.Users.Any())
                    {
                        throw new BusinessException("Cơ sở dữ liệu đã có người dùng. Không được tạo Admin lần đầu nữa.");
                    }
                    var admin = db.Roles.SingleOrDefault(x => x.RoleCode == "ADMIN" && x.IsActive);
                    if (admin == null)
                    {
                        throw new BusinessException("Chưa có nhóm quản trị ADMIN. Hãy kiểm tra cấu hình nhóm quyền trong cơ sở dữ liệu.");
                    }
                    var salt = PasswordHasher.CreateSalt();
                    var user = new UserEntity
                    {
                        Username = username, FullName = fullname, RoleId = admin.RoleId, 
                        PasswordHash = PasswordHasher.Hash(password, salt), PasswordSalt = salt, 
                        HashIterations = PasswordHasher.DefaultIterations, IsActive = true, 
                        MustChangePassword = false, FailedPasswordAttempts = 0, 
                        CreatedAt = DateTime.UtcNow, PasswordChangedAt = DateTime.UtcNow
                    };
                    db.Users.Add(user);
                    db.SaveChanges();
                    db.AuditLogs.Add(new AuditLogEntity
                    {
                        UserId = user.UserId, ActionCode = "INITIAL_ADMIN_CREATED", 
                        EntityName = "Users", EntityId = user.UserId.ToString(), 
                        Description = "Tạo quản trị viên đầu tiên trên database mới; không lưu mật khẩu.", OccurredAt = DateTime.UtcNow
                    });
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
}
