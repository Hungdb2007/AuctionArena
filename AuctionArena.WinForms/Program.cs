using System;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Data;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => 
            MessageBox.Show(e.Exception.Message, "Auction Arena", MessageBoxButtons.OK, MessageBoxIcon.Error);
            try
            {
                bool configured = ServerConfigurationForm.IsConfigured();
                if (configured)
                {
                    try
                    {
                        // Xác minh dữ liệu bằng Entity Framework; tuyệt đối không ghi xuống DB.
                        var entry = System.Configuration.ConfigurationManager.ConnectionStrings["AuctionArenaDB"];
                        // Xác minh đúng kết nối đã lưu trong cấu hình.
                        var verified = DatabaseConnectionVerifier.Resolve(entry.ConnectionString);
                        if (!string.Equals(entry.ConnectionString, verified, StringComparison.Ordinal))
                        ServerConfigurationForm.SaveVerifiedConnection(verified);
                    }
                    catch (Exception ex)
                    {
                        var answer = MessageBox.Show(
                        "Không thể sử dụng kết nối SQL Server đã lưu.\n\n" +
                        ex.Message +
                        "\n\nBạn có muốn mở màn hình cấu hình kết nối để kiểm tra lại không?", 
                        "Kết nối SQL Server", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (answer != DialogResult.Yes)
                        {
                            return;
                        }
                        configured = false;
                    }
                }
                if (!configured)
                {
                    using (var setup = new ServerConfigurationForm())
                    if (setup.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    // Mở lại tiến trình để Entity Framework đọc cấu hình kết nối vừa lưu.
                    Application.Restart();
                    return;
                }
                // Kiểm tra cấu trúc database trước khi mở Form.
                // Bộ cài (CAI_DATABASE.ps1) chịu trách nhiệm tạo/bổ sung cấu trúc; ứng dụng chỉ đọc và báo thiếu.
                try
                {
                    new SchemaReadinessService().Verify();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cấu trúc cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Khi chưa có tài khoản, mở Form tạo quản trị viên đầu tiên.
                if (new InitialAdminService().NeedsInitialAdmin())
                {
                    using (var setupAdmin = new FirstAdminForm())
                    if (setupAdmin.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, 
                "Không thể khởi động Auction Arena", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
