using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AuctionArena.Services
{
    public static class AppraisalPriceInput
    {
        // Chấp nhận số liền và nhóm hàng nghìn, không phụ thuộc vùng của Windows.
        public static decimal Parse(string input)
        {
            string text = (input ?? "").Trim().Replace('\u00a0', ' ');
            if (text.Length == 0)
                throw new BusinessException("Phải nhập giá thẩm định khi duyệt.");

            string normalized;
            if (Regex.IsMatch(text, @"^[0-9]+$"))
                normalized = text;
            else if (Regex.IsMatch(text, @"^[0-9]{1,3}(\.[0-9]{3})+(,[0-9]{1,2})?$"))
                normalized = text.Replace(".", "").Replace(',', '.');
            else if (Regex.IsMatch(text, @"^[0-9]{1,3}(,[0-9]{3})+(\.[0-9]{1,2})?$"))
                normalized = text.Replace(",", "");
            else if (Regex.IsMatch(text, @"^[0-9]{1,3}( [0-9]{3})+([.,][0-9]{1,2})?$"))
                normalized = text.Replace(" ", "").Replace(',', '.');
            else if (Regex.IsMatch(text, @"^[0-9]+[.,][0-9]{1,2}$"))
                normalized = text.Replace(',', '.');
            else
                throw new BusinessException("Giá thẩm định không hợp lệ. Ví dụ: 15000000 hoặc 15.000.000; tối đa 2 chữ số thập phân.");

            decimal price;
            if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price) ||
                price <= 0 || price > 99999999999999999.99m)
                throw new BusinessException("Giá thẩm định phải lớn hơn 0 và không vượt 99.999.999.999.999.999,99.");
            return price;
        }
    }
}
