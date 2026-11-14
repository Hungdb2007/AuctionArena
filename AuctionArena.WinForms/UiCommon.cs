using System;
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
            column.Width = width;
            column.Tag = "AA_FixedWidth";
        }
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
