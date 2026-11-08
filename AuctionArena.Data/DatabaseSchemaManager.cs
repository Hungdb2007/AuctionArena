using System;
using System.Data.SqlClient;
using System.IO;

namespace AuctionArena.Data
{
    public static class DatabaseSchemaManager
    {
        // Tệp SQL được nhúng trong DLL để chạy được từ Visual Studio và bản cài đặt.
        // Chạy SQL đồng bộ cấu trúc nhúng và diễn giải lỗi kết nối/quyền. (10-51)
        public static void EnsurePaymentAndDeliverySchema(string connectionString)
        {
            var options = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(options.InitialCatalog))
                throw new InvalidOperationException("Chưa chọn cơ sở dữ liệu Auction Arena.");

            string sql;
            using (var stream = typeof(DatabaseSchemaManager).Assembly.GetManifestResourceStream(
                "AuctionArena.Data.PaymentDeliverySchema.sql"))
            {
                if (stream == null)
                    throw new InvalidOperationException("Thiếu tài nguyên cấu trúc cơ sở dữ liệu. Hãy biên dịch hoặc cài lại Auction Arena.");
                using (var reader = new StreamReader(stream))
                    sql = reader.ReadToEnd();
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.CommandTimeout = 120;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                string action = ex.Number == 229 || ex.Number == 262 || ex.Number == 1088 || ex.Number == 15151
                    ? "Tài khoản kết nối chưa đủ quyền sửa cấu trúc. Quản trị viên cần chạy " +
                      "SQL/04_DongBoThanhToanXuatKho.sql bằng tài khoản có quyền ALTER, sau đó mở lại ứng dụng."
                    : "Hãy kiểm tra lỗi SQL bên dưới. Quản trị viên có thể chạy " +
                      "SQL/04_DongBoThanhToanXuatKho.sql trên đúng cơ sở dữ liệu.";
                throw new InvalidOperationException(
                    "Không thể chuẩn bị cấu trúc thanh toán và xuất kho.\n" +
                    "Server: " + options.DataSource + "\nDatabase: " + options.InitialCatalog +
                    "\n\n" + action + "\n\nChi tiết: " + ex.Message, ex);
            }
        }
    }
}
