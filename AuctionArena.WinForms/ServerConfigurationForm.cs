using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Data.SqlClient;
using System.Windows.Forms;
using AuctionArena.Services;
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
}
