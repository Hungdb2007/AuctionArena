using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace AuctionArena.WinForms
{
    // Muốn đổi màu hoặc kích thước nút: sửa ở đây, không sửa từng Form.
    // Các hàm Label, Input, Button, Grid tạo điều khiển dùng chung cho các Form.
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(244, 247, 251);
        public static readonly Color Panel = Color.White;
        // Màu nút theo thao tác: xanh dương (mặc định/Sửa), xanh lá (Thêm/Lưu/Tạo), đỏ (Xóa/Hủy/Khóa).
        public static readonly Color Primary = Color.FromArgb(25, 118, 210);
        public static readonly Color PrimaryHover = Color.FromArgb(21, 101, 192);
        public static readonly Color Success = Color.FromArgb(46, 125, 50);
        public static readonly Color SuccessHover = Color.FromArgb(56, 142, 60);
        public static readonly Color Danger = Color.FromArgb(198, 40, 40);
        public static readonly Color DangerHover = Color.FromArgb(211, 47, 47);
        // Dòng tiêu đề của bảng.
        public static readonly Color HeaderBg = Color.FromArgb(25, 118, 210);
        public static readonly Color HeaderText = Color.White;
        public static readonly Color DisabledBg = Color.FromArgb(225, 229, 235);
        public static readonly Color DisabledBorder = Color.FromArgb(200, 206, 214);
        public static readonly Color Accent = Primary;
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color Muted = Color.FromArgb(95, 109, 127);
        private static readonly Color Border = Color.FromArgb(213, 222, 233);
        private static readonly Color RowAlternate = Color.FromArgb(248, 250, 253);
        private static readonly Color Selected = Color.FromArgb(213, 231, 248);
        // Đặt font và màu chung trước khi tạo các điều khiển.
        public static void Apply(Control form)
        {
            form.BackColor = Bg;
            form.ForeColor = Text;
            form.Font = new Font("Segoe UI", 10);
        }
        // Các ô nhập và nút được tạo bằng hàm chung để dễ bảo trì.
        public static Label Label(string caption, int x, int y, int width = 250)
        {
            return new Label
            {
                Text = caption, 
                Left = x, 
                Top = y, 
                Width = width, 
                Height = 27, 
                ForeColor = Text, 
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
        public static TextBox Input(int x, int y, int width = 250, bool secret = false)
        {
            return new TextBox
            {
                Left = x, 
                Top = y, 
                Width = width, 
                BackColor = Color.White, 
                ForeColor = Text, 
                BorderStyle = BorderStyle.FixedSingle, 
                UseSystemPasswordChar = secret
            };
        }
        public static Button Button(string caption, int x, int y, int width = 160)
        {
            Color back, hover;
            ButtonColors(caption, out back, out hover);
            var button = new Button
            {
                Text = caption,
                Left = x,
                Top = y,
                Width = width,
                Height = 38,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = back;
            button.FlatAppearance.MouseOverBackColor = hover;
            // Nút bị khóa: Windows vẽ chữ xám đậm, nên đổi nền sang xám nhạt cho dễ đọc và dễ nhận biết.
            button.EnabledChanged += (s, e) =>
            {
                button.BackColor = button.Enabled ? back : DisabledBg;
                button.FlatAppearance.BorderColor = button.Enabled ? back : DisabledBorder;
            };
            return button;
        }
        // Chọn màu nút theo chữ trên nút: Thêm/Lưu/Tạo = xanh lá, Xóa/Hủy/Khóa = đỏ, còn lại = xanh dương.
        static void ButtonColors(string caption, out Color back, out Color hover)
        {
            string text = (caption ?? "").Trim().ToLowerInvariant();
            bool clearFilter = text.StartsWith("xóa lọc") || text.StartsWith("xóa bộ lọc");
            if (!clearFilter && (text.StartsWith("xóa") || text.StartsWith("hủy") ||
                text.StartsWith("khóa") || text.StartsWith("từ chối") || text.StartsWith("bỏ cọc")))
            {
                back = Danger;
                hover = DangerHover;
            }
            else if (text.StartsWith("thêm") || text.StartsWith("lưu") || text.StartsWith("tạo") ||
                text.StartsWith("lập") || text.StartsWith("nhóm mới") || text.StartsWith("ký"))
            {
                back = Success;
                hover = SuccessHover;
            }
            else
            {
                back = Primary;
                hover = PrimaryHover;
            }
        }
        // Bảng chỉ dùng để chọn dòng; việc sửa dữ liệu thực hiện qua các nút của Form.
        public static DataGridView Grid(int x, int y, int width, int height)
        {
            var grid = new DataGridView
            {
                Left = x, 
                Top = y, 
                Width = width, 
                Height = height, 
                ReadOnly = true, 
                AutoGenerateColumns = true, 
                AllowUserToAddRows = false, 
                AllowUserToDeleteRows = false, 
                AllowUserToResizeColumns = false, 
                AllowUserToResizeRows = false, 
                AllowUserToOrderColumns = false, 
                RowHeadersVisible = false, 
                MultiSelect = false, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, 
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, 
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, 
                ColumnHeadersHeight = 38, 
                RowTemplate = 
                {
                    Height = 31
                }, 
                ScrollBars = ScrollBars.Both, 
                EnableHeadersVisualStyles = false, 
                BorderStyle = BorderStyle.FixedSingle, 
                BackgroundColor = Panel, 
                GridColor = Border
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White, 
                ForeColor = Text, 
                SelectionBackColor = Selected, 
                SelectionForeColor = Text, 
                Font = new Font("Segoe UI", 10), 
                Padding = new Padding(5, 0, 5, 0)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlternate;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBg,
                ForeColor = HeaderText,
                SelectionBackColor = HeaderBg,
                SelectionForeColor = HeaderText,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), 
                Alignment = DataGridViewContentAlignment.MiddleLeft, 
                Padding = new Padding(5, 0, 5, 0)
            };
            // DataSource thay đổi thì các cột tự sinh cũng phải được định dạng lại.
            grid.ColumnAdded += (sender, args) => StyleColumn(args.Column);
            grid.DataBindingComplete += (sender, args) => 
            {
                HideBinaryColumns(grid);
                foreach (DataGridViewColumn column in grid.Columns)
                StyleColumn(column);
            };
            grid.DataError += (sender, args) => 
            {
                // Một giá trị hiển thị lỗi không được làm sập toàn bộ Form.
                System.Diagnostics.Debug.WriteLine("Grid: " + args.Exception);
                args.ThrowException = false;
            };
            return grid;
        }
        private static void StyleColumn(DataGridViewColumn column)
        {
            if (column == null)
            return;
            // Khóa kích thước cột và chọn màu chữ thống nhất.
            column.Resizable = DataGridViewTriState.False;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.DefaultCellStyle.BackColor = Color.White;
            column.DefaultCellStyle.ForeColor = Text;
            column.DefaultCellStyle.SelectionBackColor = Selected;
            column.DefaultCellStyle.SelectionForeColor = Text;
            column.HeaderCell.Style.BackColor = HeaderBg;
            column.HeaderCell.Style.ForeColor = HeaderText;
            column.HeaderCell.Style.SelectionBackColor = HeaderBg;
            column.HeaderCell.Style.SelectionForeColor = HeaderText;
            // Cột đã đặt độ rộng rồi thì không thay đổi nữa.
            if (Convert.ToString(column.Tag) == "AA_FixedWidth")
            return;
            string name = ((column.DataPropertyName ?? "") + " " +
            (column.HeaderText ?? "")).ToLowerInvariant();
            FormatColumn(column);
            int width = 150;
            if (name.Contains("description") || name.Contains("mô tả") || name.Contains("ghi chú"))
            width = 270;
            else if (name.Contains("itemname") || name.Contains("fullname") || 
            name.Contains("customer") || name.Contains("khách") || name.Contains("hiện vật"))
            width = 225;
            else if (name.Contains("date") || name.Contains("createdat") || name.Contains("paidat"))
            width = 178;
            else if (name.Contains("amount") || name.Contains("price") || name.Contains("tiền"))
            width = 162;
            else if (name.EndsWith("id") || name.Contains("id "))
            width = 95;
            else if (name.Contains("code") || name.Contains("mã"))
            width = 140;
            // Những bảng khai báo cột bằng tay giữ đúng độ rộng do Form đã đặt.
            if (column.DataGridView != null && !column.DataGridView.AutoGenerateColumns)
            width = column.Width;
            else
            {
                // Tiêu đề tiếng Việt dài hơn tên thuộc tính: nới cột vừa đủ để không bị cắt chữ.
                int need = TextRenderer.MeasureText(column.HeaderText ?? "", HeaderFont).Width + 18;
                if (need > width)
                width = need;
            }
            column.Width = width;
            column.Tag = "AA_FixedWidth";
        }
        private static readonly Font HeaderFont = new Font("Segoe UI", 10, FontStyle.Bold);
        // Tiền: phân cách hàng nghìn, bỏ phần lẻ .00 (1000000.00 -> 1,000,000). Dùng culture hiện tại như N0 ở các nhãn.
        public const string MoneyFormat = "#,##0.##";
        public const string DateTimeFormat = "dd/MM/yyyy HH:mm";
        public const string DateFormat = "dd/MM/yyyy";
        // Cột chỉ lưu ngày (không có giờ).
        private static readonly string[] DateOnlyColumns = { "DateOfBirth", "ExpiresAt" };
        // Định dạng số/ngày và đổi tiêu đề tiếng Việt cho cột tự sinh; cột Form đã tự đặt thì giữ nguyên.
        private static void FormatColumn(DataGridViewColumn column)
        {
            string property = string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;
            if (string.IsNullOrEmpty(property))
            return;
            string vietnamese;
            if ((column.HeaderText == property || column.HeaderText == column.Name) &&
                Headers.TryGetValue(property, out vietnamese))
            column.HeaderText = vietnamese;
            if (!string.IsNullOrEmpty(column.DefaultCellStyle.Format) || column.ValueType == null)
            return;
            var type = Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType;
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            {
                // SuccessRate đã là số phần trăm (0-100); các Rate khác lưu dạng 0.08 = 8%.
                if (property == "SuccessRate" || property.EndsWith("Percent"))
                column.DefaultCellStyle.Format = "0.##'%'";
                else if (property.Contains("Rate"))
                column.DefaultCellStyle.Format = "0.##%";
                else
                column.DefaultCellStyle.Format = MoneyFormat;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            else if (type == typeof(DateTime))
            {
                column.DefaultCellStyle.Format = Array.IndexOf(DateOnlyColumns, property) >= 0 ? DateFormat : DateTimeFormat;
            }
        }
        // Tên thuộc tính -> tiêu đề cột tiếng Việt. Tên không có trong bảng thì giữ nguyên.
        private static readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Số định danh (Id) ghi "ID ..." để không trùng với cột Mã (Code).
            { "Id", "ID" }, { "Code", "Mã" }, { "UserId", "ID tài khoản" }, { "RoleId", "ID nhóm" }, { "PermissionId", "ID quyền" },
            { "CustomerId", "ID khách" }, { "ConsignorId", "ID chủ hàng" }, { "CategoryId", "ID danh mục" }, { "ItemId", "ID hiện vật" },
            { "ItemImageId", "ID ảnh" }, { "ContractId", "ID hợp đồng" }, { "ContractDetailId", "ID dòng HĐ" },
            { "ReceiptId", "ID phiếu nhập" }, { "ReceiptDetailId", "ID dòng nhập" }, { "SessionId", "ID phiên" },
            { "LotId", "ID lot" }, { "BidId", "ID lượt giá" }, { "RegistrationId", "ID đăng ký" },
            { "InvoiceId", "ID hóa đơn" }, { "InvoiceDetailId", "ID dòng hóa đơn" }, { "PaymentId", "ID phiếu thu" },
            { "PayoutId", "ID chi trả" }, { "OrderId", "ID lệnh" }, { "ObligationId", "ID nghĩa vụ" },
            { "IssueId", "ID phiếu xuất" }, { "IssueDetailId", "ID dòng xuất" }, { "ReturnId", "ID phiếu trả" },
            { "AdjustmentId", "ID điều chỉnh" }, { "ViolationId", "ID vi phạm" }, { "ReviewId", "ID xét duyệt" },
            { "HistoryId", "ID lịch sử" }, { "AuditLogId", "ID nhật ký" }, { "EntityId", "ID đối tượng" },
            { "LeadingCustomerId", "Khách dẫn đầu" }, { "WinningCustomerId", "Khách thắng" },
            { "CreatedByUserId", "Người tạo" }, { "ChangedByUserId", "Người đổi" }, { "AppraisedByUserId", "Người thẩm định" },
            { "ApprovedByUserId", "Người duyệt" }, { "EnteredByUserId", "Người nhập" }, { "IssuedByUserId", "Người xuất" },
            { "PaidByUserId", "Người chi" }, { "ReceivedByUserId", "Người nhận" }, { "ReturnedByUserId", "Người trả" },
            { "CustomerCode", "Mã khách" }, { "ConsignorCode", "Mã chủ hàng" }, { "CategoryCode", "Mã danh mục" },
            { "ItemCode", "Mã hiện vật" }, { "ContractCode", "Mã hợp đồng" }, { "ReceiptCode", "Mã phiếu nhập" },
            { "SessionCode", "Mã phiên" }, { "InvoiceCode", "Mã hóa đơn" }, { "PaymentCode", "Mã phiếu thu" },
            { "PayoutCode", "Mã chi trả" }, { "IssueCode", "Mã phiếu xuất" }, { "ReturnCode", "Mã phiếu trả" },
            { "AdjustmentCode", "Mã điều chỉnh" }, { "RoleCode", "Mã nhóm" }, { "PermissionCode", "Mã quyền" },
            { "ActionCode", "Mã thao tác" },
            // Tên, mô tả
            { "Name", "Tên" }, { "FullName", "Họ tên" }, { "Username", "Tên đăng nhập" }, { "RoleName", "Nhóm quyền" },
            { "PermissionName", "Quyền" }, { "CustomerName", "Khách hàng" }, { "Customer", "Khách hàng" },
            { "ConsignorName", "Chủ hàng" }, { "Consignor", "Chủ hàng" }, { "Owner", "Chủ hàng" }, { "OwnerName", "Chủ hàng" },
            { "CategoryName", "Danh mục" }, { "Category", "Danh mục" }, { "ItemName", "Tên hiện vật" }, { "Item", "Hiện vật" },
            { "SessionName", "Phiên" }, { "Session", "Phiên" }, { "Contract", "Hợp đồng" }, { "Label", "Nhãn" },
            { "Display", "Hiển thị" }, { "Group", "Nhóm" }, { "GroupLabel", "Nhóm" }, { "GroupBy", "Nhóm theo" },
            { "Description", "Mô tả" }, { "Notes", "Ghi chú" }, { "Detail", "Chi tiết" }, { "Reason", "Lý do" },
            { "RepresentativeName", "Người đại diện" }, { "RecipientName", "Người nhận" }, { "Recipient", "Người nhận" },
            { "RecipientDocument", "Giấy tờ người nhận" }, { "RecipientRelation", "Quan hệ" },
            { "DeliveredBy", "Người giao" }, { "ReceivedBy", "Người nhận" },
            // Liên hệ, hồ sơ
            { "Email", "Email" }, { "Phone", "Điện thoại" }, { "Address", "Địa chỉ" }, { "Gender", "Giới tính" },
            { "DateOfBirth", "Ngày sinh" }, { "IdentityNumber", "Số CCCD" }, { "MemberTier", "Hạng TV" },
            { "OldTier", "Hạng cũ" }, { "NewTier", "Hạng mới" }, { "BankName", "Ngân hàng" },
            { "BankAccount", "Số tài khoản" }, { "BankAccountHolder", "Chủ tài khoản" }, { "ConsignorType", "Loại chủ hàng" },
            { "Paddle", "Paddle" }, { "PaddleNumber", "Paddle" }, { "BlacklistReason", "Lý do chặn" },
            // Hiện vật
            { "Origin", "Xuất xứ" }, { "Era", "Niên đại" }, { "Dimensions", "Kích thước" }, { "Material", "Chất liệu" },
            { "Condition", "Tình trạng" }, { "ConditionDescription", "Tình trạng" }, { "ConditionAtReceipt", "Tình trạng khi nhận" },
            { "ConditionAtDelivery", "Tình trạng khi giao" }, { "Location", "Vị trí" }, { "WarehouseLocation", "Vị trí kho" },
            { "LocationAtReceipt", "Vị trí khi nhận" }, { "LocationAtIssue", "Vị trí khi xuất" }, { "AppraisalNotes", "Ghi chú thẩm định" },
            { "PhotoPath", "Ảnh" }, { "RelativePath", "Đường dẫn" }, { "IsPrimary", "Ảnh chính" }, { "SortOrder", "Thứ tự" },
            { "LotNumber", "Số lot" },
            // Tiền
            { "Amount", "Số tiền" }, { "Price", "Giá" }, { "StartingPrice", "Giá khởi điểm" }, { "ReservePrice", "Giá sàn" },
            { "CurrentPrice", "Giá hiện tại" }, { "HammerPrice", "Giá búa" }, { "BidAmount", "Giá trả" }, { "BidIncrement", "Bước giá" },
            { "EstimatePrice", "Giá ước tính" }, { "EstimatePriceHigh", "Ước tính tối đa" }, { "Estimated", "Giá ước tính" },
            { "AppraisedPrice", "Giá thẩm định" }, { "HammerSubtotal", "Tổng giá búa" }, { "HammerTotal", "Tổng giá búa" },
            { "BuyerFee", "Phí người mua" }, { "SellerFee", "Phí chủ hàng" }, { "Discount", "Giảm giá" },
            { "VipDiscount", "Giảm giá VIP" }, { "Vat", "VAT" }, { "VatAmount", "Tiền VAT" }, { "TotalAmount", "Tổng tiền" },
            { "TotalDue", "Phải thu" }, { "DueAmount", "Phải trả" }, { "PaidAmount", "Đã trả" }, { "Paid", "Đã trả" },
            { "Remaining", "Còn lại" }, { "RemainingDebt", "Còn nợ" }, { "RemainingAtReview", "Còn nợ khi xét" },
            { "Outstanding", "Còn nợ" }, { "UnpaidAmount", "Chưa thu" }, { "TotalSpent", "Tổng chi tiêu" },
            { "DepositRequired", "Cọc yêu cầu" }, { "DepositPaid", "Đã cọc" }, { "DepositApplied", "Cọc đã trừ" },
            { "DepositForfeited", "Cọc bị phạt" }, { "DepositRefunded", "Cọc đã hoàn" }, { "AppliedDeposit", "Cọc đã trừ" },
            { "HeldDeposit", "Cọc đang giữ" }, { "AlreadyForfeited", "Đã phạt cọc" }, { "AdditionalForfeiture", "Phạt thêm" },
            { "TotalForfeiture", "Tổng phạt" }, { "PayoutAmount", "Số tiền chi" }, { "PaidToSeller", "Đã trả chủ hàng" },
            { "PendingSellerPayout", "Chờ trả chủ hàng" }, { "PlatformRevenue", "Doanh thu sàn" }, { "Revenue", "Doanh thu" },
            { "PreviousRevenue", "Doanh thu kỳ trước" },
            // Tỷ lệ
            { "SellerFeeRate", "Phí chủ hàng (%)" }, { "SellerFeeRateLocked", "Phí chủ hàng (%)" },
            { "BuyerFeeRate", "Phí người mua (%)" }, { "VatRate", "VAT (%)" }, { "VipDiscountRate", "Giảm VIP (%)" },
            { "SuccessRate", "Tỷ lệ bán (%)" },
            // Số lượng
            { "Count", "Số lượng" }, { "Items", "Số hiện vật" }, { "InStock", "Trong kho" }, { "Contracts", "Số hợp đồng" },
            { "Invoices", "Số hóa đơn" }, { "InvoiceCount", "Số hóa đơn" }, { "UnpaidInvoices", "HĐơn chưa thu" },
            { "LotCount", "Số lot" }, { "LotsWon", "Số lot thắng" }, { "WonLots", "Số lot thắng" }, { "SoldLots", "Lot đã bán" },
            { "UnsoldLots", "Lot không bán" }, { "Registrations", "Lượt đăng ký" }, { "CustomerCount", "Số khách" },
            { "NewCustomers", "Khách mới" }, { "OpenSessions", "Phiên đang mở" }, { "UpcomingSessions", "Phiên sắp tới" },
            { "FailedPasswordAttempts", "Lần sai MK" }, { "HashIterations", "Số vòng băm" }, { "Month", "Tháng" }, { "Year", "Năm" },
            // Thời gian
            { "CreatedAt", "Ngày tạo" }, { "UpdatedAt", "Ngày cập nhật" }, { "ChangedAt", "Thời điểm đổi" },
            { "Date", "Ngày" }, { "Time", "Thời điểm" }, { "At", "Thời điểm" }, { "When", "Thời điểm" },
            { "OccurredAt", "Thời điểm" }, { "From", "Từ ngày" }, { "To", "Đến ngày" }, { "StartsAt", "Bắt đầu" },
            { "EndsAt", "Kết thúc" }, { "OpensAt", "Mở lúc" }, { "ClosesAt", "Đóng lúc" }, { "BidAt", "Thời điểm trả giá" },
            { "RegisteredAt", "Ngày đăng ký" }, { "SignedAt", "Ngày ký" }, { "ExpiresAt", "Ngày hết hạn" },
            { "ReceivedAt", "Ngày nhận" }, { "IssuedAt", "Ngày lập" }, { "DueAt", "Hạn thanh toán" }, { "PaidAt", "Ngày trả" },
            { "PayoutAt", "Ngày chi" }, { "SettledAt", "Ngày tất toán" }, { "ResolvedAt", "Ngày xử lý" },
            { "ReturnedAt", "Ngày trả hàng" }, { "AppraisedAt", "Ngày thẩm định" }, { "LastLoginAt", "Đăng nhập cuối" },
            { "PasswordChangedAt", "Đổi MK lúc" }, { "LockedUntil", "Khóa đến" },
            // Trạng thái
            { "Status", "Trạng thái" }, { "State", "Trạng thái" }, { "Phase", "Giai đoạn" }, { "Result", "Kết quả" },
            { "Outcome", "Kết quả" }, { "Review", "Cảnh báo" }, { "ReviewStatus", "Trạng thái xét" }, { "ReviewReason", "Lý do xét" },
            { "ItemStatus", "Trạng thái" }, { "LotStatus", "Trạng thái lot" }, { "SessionStatus", "Trạng thái phiên" },
            { "ContractStatus", "Trạng thái HĐ" }, { "CustomerStatus", "Trạng thái" }, { "ConsignorStatus", "Trạng thái" },
            { "InvoiceStatus", "Trạng thái HĐơn" }, { "PaymentStatus", "Thanh toán" }, { "RegistrationStatus", "Trạng thái ĐK" },
            { "ObligationStatus", "Trạng thái" }, { "OrderStatus", "Trạng thái lệnh" }, { "PayoutOrderStatus", "Trạng thái chi" },
            { "WorkflowStatus", "Tiến trình" }, { "IsActive", "Hoạt động" }, { "Active", "Hoạt động" },
            { "IsBlacklisted", "Bị chặn" }, { "HasOutstandingDebt", "Còn nợ" }, { "MustChangePassword", "Phải đổi MK" },
            { "LockReason", "Lý do khóa" }, { "Selected", "Chọn" },
            // Thanh toán, nhật ký
            { "Type", "Loại" }, { "PaymentMethod", "Hình thức" }, { "PaymentType", "Loại thanh toán" },
            { "DeliveryMethod", "Cách giao" }, { "AdjustmentType", "Loại điều chỉnh" }, { "ViolationType", "Loại vi phạm" },
            { "TransactionReference", "Mã giao dịch" }, { "TransferReference", "Mã chuyển khoản" },
            { "Action", "Thao tác" }, { "EntityName", "Đối tượng" }, { "TableName", "Bảng" }, { "TableSchema", "Schema" },
            { "ColumnName", "Cột" }, { "SettingKey", "Tham số" }, { "SettingValue", "Giá trị" },
            // Quyền
            { "View", "Xem" }, { "Create", "Thêm" }, { "Edit", "Sửa" }, { "Delete", "Xóa" }, { "Print", "In" },
            { "CanView", "Xem" }, { "CanCreate", "Thêm" }, { "CanEdit", "Sửa" }, { "CanDelete", "Xóa" }, { "CanPrint", "In/Xuất" }
        };
        private static void HideBinaryColumns(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                // byte[] và RowVersion dùng cho nghiệp vụ, không phải nội dung để đọc.
                if (column is DataGridViewImageColumn || 
                column.ValueType == typeof(byte[]) || 
                string.Equals(column.DataPropertyName, "RowVersion", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(column.Name, "RowVersion", StringComparison.OrdinalIgnoreCase))
                column.Visible = false;
            }
        }
        // Hộp nhập lý do dùng chung cho hành động khóa, hủy chứng từ..
        public static string AskReason(IWin32Window owner, string title)
        {
            using (var dialog = new Form
            {
                Text = title, 
                Width = 520, 
                Height = 235, 
                StartPosition = FormStartPosition.CenterParent, 
                FormBorderStyle = FormBorderStyle.FixedDialog, 
                MaximizeBox = false
            })
            {
                Apply(dialog);
                dialog.Controls.Add(Label(title, 18, 18, 465));
                var field = Input(18, 55, 465);
                field.MaxLength = 250;
                dialog.Controls.Add(field);
                var ok = Button("Xác nhận", 18, 108, 180);
                var cancel = Button("Bỏ qua", 215, 108, 160);
                ok.DialogResult = DialogResult.OK;
                cancel.DialogResult = DialogResult.Cancel;
                dialog.Controls.Add(ok);
                dialog.Controls.Add(cancel);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                return dialog.ShowDialog(owner) == DialogResult.OK ? field.Text.Trim() : null;
            }
        }
        // Khi Service báo lỗi, hiển thị tiếng Việt thay vì đóng ứng dụng.
        public static void Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // EF có thể bọc lỗi SQL trong nhiều lớp Exception.
                // Lấy lỗi gốc để thấy chính xác tên cột/bảng hoặc lý do SQL từ chối.
                var root = ex.GetBaseException();
                string message = ex.Message;
                if (root != null && root != ex && 
                    !string.Equals(root.Message, ex.Message, StringComparison.Ordinal))
                {
                    message += "\n\nNguyên nhân chi tiết: " + root.Message;
                }
                MessageBox.Show(message, "Không thể thực hiện", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
