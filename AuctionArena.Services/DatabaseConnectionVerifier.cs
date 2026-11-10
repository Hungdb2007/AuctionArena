using System;
using System.Data.SqlClient;
using System.Linq;
using AuctionArena.Data;
namespace AuctionArena.Services
{
    // Xác minh kết nối và các bảng bắt buộc trước khi lưu cấu hình.
    public static class DatabaseConnectionVerifier
    {
        public static void Verify(string connectionString)
        {
            var options = new SqlConnectionStringBuilder(connectionString);
            try
            {
                // Mở kết nối SQL thật để phân biệt lỗi kết nối và lỗi Entity Framework.
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    if (!string.Equals(connection.Database, options.InitialCatalog, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Kết nối đang trỏ tới database không đúng.");
                    }
                }
                using (var db = new AuctionArenaDbContext(connectionString))
                {
                    // Đọc cột bắt buộc để xác nhận đúng cấu trúc AuctionArenaDB.
                    // Any() một mình chỉ kiểm tra bảng tồn tại và có thể bỏ sót cột thiếu.
                    var users = db.Users.Select(u => new
                    {
                        u.UserId, u.Username, u.PasswordHash, 
                        u.PasswordSalt, u.HashIterations, u.RoleId, u.IsActive
                    }).Take(1).ToList();
                    if (users.Count == 0 && !db.Roles.Any(r => r.RoleCode == "ADMIN" && r.IsActive))
                    throw new InvalidOperationException(
                    "Bảng Users chưa có tài khoản và cũng chưa tồn tại nhóm ADMIN để khởi tạo.");
                    // Database chưa có tài khoản sẽ mở màn hình tạo quản trị viên.
                    // Kiểm tra các bảng nghiệp vụ bắt buộc.
                    db.Roles.Select(r => r.RoleId).Take(1).ToList();
                    db.Permissions.Select(p => p.PermissionId).Take(1).ToList();
                    db.Invoices.Select(i => i.InvoiceId).Take(1).ToList();
                    db.Registrations.Select(r => r.RegistrationId).Take(1).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                "Server: " + options.DataSource + "\n" +
                "Database: " + options.InitialCatalog + "\n" +
                "Không xác minh được cấu trúc cơ sở dữ liệu Auction Arena.\n" +
                "Chi tiết: " + ex.GetBaseException().Message + "\n\n" +
                "Hãy kiểm tra tên SQL Server, cơ sở dữ liệu và quyền truy cập. " +
                "Nếu cấu trúc còn thiếu, liên hệ quản trị viên để kiểm tra và sao lưu dữ liệu. " +
                "Không chạy tệp tạo cơ sở dữ liệu trên dữ liệu hiện có.", ex);
            }
        }
        // Chi su dung ten SQL Server do nguoi cai dat chon.
        public static string Resolve(string connectionString)
        {
            Verify(connectionString);
            return connectionString;
        }
    }
}
