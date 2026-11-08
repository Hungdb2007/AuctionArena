using System;
using System.Security.Cryptography;

namespace AuctionArena.Data.Security
{
    public static class PasswordHasher
    {
        public const int DefaultIterations = 150000; 
        // Tạo salt bằng bộ sinh số ngẫu nhiên mật mã; dẫn xuất hash bằng PBKDF2-HMAC-SHA256. Hash và salt được đổi sang hex; Verify so sánh toàn bộ các byte thay vì dừng ở byte khác đầu tiên
        // Tương thích dữ liệu nhập trước đây: salt 16 byte và hash 32 byte lưu dưới dạng hex.
             public static string CreateSalt()
        {
            var salt = new byte[16]; // Tạo salt ngẫu nhiên 16 byte.
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return BitConverter.ToString(salt).Replace("-", "");
        }
        public static string Hash(string password, string saltHex, int iterations = DefaultIterations)
        {
            // Dẫn xuất hash PBKDF2-HMAC-SHA256.
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Mật khẩu không được để trống.");
            }
            if (iterations < 100000)
            {
                throw new ArgumentOutOfRangeException("iterations");
            }
            byte[] salt = HexToBytes(saltHex);
            if (salt.Length != 16)
            {
                throw new ArgumentException("Salt phải có 16 byte.");
            }
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return BitConverter.ToString(pbkdf2.GetBytes(32)).Replace("-", "");
        }
        public static bool Verify(string password, string saltHex, string expectedHex, int iterations)
        {
            // Tính lại hash và so sánh các byte với hash lưu
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(expectedHex))
            {
                return false;
            }
            var actual = HexToBytes(Hash(password, saltHex, iterations));
            var expected = HexToBytes(expectedHex);
            if (actual.Length != expected.Length)
            {
                return false;
            }
            int diff = 0;
            for (int i = 0; i < actual.Length; i++) diff |= actual[i] ^ expected[i];
            return diff == 0;
        }
        private static byte[] HexToBytes(string hex)
        {
            // Chuyển chuỗi hex thành mảng byte
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
            {
                throw new ArgumentException("Chuỗi hex không hợp lệ.");
            }
            byte[] data = new byte[hex.Length / 2];
            for (int i = 0; i < data.Length; i++) data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return data;
        }
    }
}
