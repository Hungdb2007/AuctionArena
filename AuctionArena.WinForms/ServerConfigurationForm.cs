using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Data.SqlClient;
using System.Windows.Forms;
using AuctionArena.Data;
namespace AuctionArena.WinForms
{
    public sealed partial class ServerConfigurationForm : Form
    {
        public ServerConfigurationForm()
        {
            InitializeComponent();
            Theme.Apply(this);
            btnSave.BackColor = Theme.Primary;
            btnSave.ForeColor = System.Drawing.Color.White;
            LoadCurrentConnection();
            UpdateAuthenticationFields();
        }

        private void LoadCurrentConnection()
        {
            var existing = ConfigurationManager.ConnectionStrings["AuctionArenaDB"];
            if (existing == null || string.IsNullOrWhiteSpace(existing.ConnectionString))
            {
                database.Text = "AuctionArenaDB";
                return;
            }

            try
            {
                var config = new SqlConnectionStringBuilder(existing.ConnectionString);
                server.Text = config.DataSource;
                database.Text = config.InitialCatalog;
                integrated.Checked = config.IntegratedSecurity;
                if (!integrated.Checked)
                {
                    username.Text = config.UserID;
                }
            }
            catch (ArgumentException)
            {
                database.Text = "AuctionArenaDB";
            }
        }

        private void Integrated_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAuthenticationFields();
        }

        private void UpdateAuthenticationFields()
        {
            username.Enabled = !integrated.Checked;
            password.Enabled = !integrated.Checked;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Theme.Safe(Save);
        }

        public static bool IsConfigured()
        {
            var entry = ConfigurationManager.ConnectionStrings["AuctionArenaDB"];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
            {
                return false;
            }
            try
            {
                var value = new SqlConnectionStringBuilder(entry.ConnectionString);
                return ConfigurationManager.AppSettings["ConnectionConfigured"] == "true" && 
                !string.IsNullOrWhiteSpace(value.DataSource) && 
                !string.IsNullOrWhiteSpace(value.InitialCatalog);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(server.Text) || string.IsNullOrWhiteSpace(database.Text))
            throw new InvalidOperationException("Hãy nhập tên Server và Database.");
            var value = new SqlConnectionStringBuilder
            {
                DataSource = server.Text.Trim(), 
                InitialCatalog = database.Text.Trim(), 
                IntegratedSecurity = integrated.Checked, 
                MultipleActiveResultSets = true, 
                ApplicationName = "AuctionArena", 
                ConnectTimeout = 4
            };
            if (!integrated.Checked)
            {
                if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(password.Text))
                throw new InvalidOperationException("Hãy nhập tài khoản và mật khẩu SQL Server.");
                value.UserID = username.Text.Trim();
                value.Password = password.Text;
            }
            // Chỉ dùng kết nối người cài đặt vừa nhập.
            var verifiedConnectionString = DatabaseConnectionVerifier.Resolve(value.ConnectionString);
            SaveVerifiedConnection(verifiedConnectionString);
            MessageBox.Show(
            "Đã xác minh cơ sở dữ liệu và tài khoản đăng nhập.\n" +
            "Server đã lưu: " + new SqlConnectionStringBuilder(verifiedConnectionString).DataSource +
            "\n\nỨng dụng sẽ mở lại với kết nối vừa lưu.", 
            "Auction Arena", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        // Chỉ gọi sau khi xác minh dữ liệu thành công. Không ghi vào database.
        public static void SaveVerifiedConnection(string connectionString)
        {
            var value = new SqlConnectionStringBuilder(connectionString);
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var entry = config.ConnectionStrings.ConnectionStrings["AuctionArenaDB"];
            if (entry == null)
            config.ConnectionStrings.ConnectionStrings.Add(
            new ConnectionStringSettings("AuctionArenaDB", value.ConnectionString, "System.Data.SqlClient"));
            else
            {
                entry.ConnectionString = value.ConnectionString;
                entry.ProviderName = "System.Data.SqlClient";
            }
            var setting = config.AppSettings.Settings["ConnectionConfigured"];
            if (setting == null) config.AppSettings.Settings.Add("ConnectionConfigured", "true");
            else setting.Value = "true";
            // Với SQL Authentication, mã hóa mục connectionStrings bằng DPAPI.
            if (!value.IntegratedSecurity && !config.ConnectionStrings.SectionInformation.IsProtected)
            config.ConnectionStrings.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("connectionStrings");
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
    // Xác minh kết nối và các bảng bắt buộc trước khi lưu cấu hình.
    internal static class DatabaseConnectionVerifier
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
