using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
   //Form nền cho nhóm màn hình đấu giá. Gom lưới, trường nhập, nút thao tác, chọn ID và kiểm tra số nguyên/số tiền

    public abstract class AuctionFormBase : Form
    {
        protected readonly int Actor;
        protected readonly PermissionFlags Flags;
        protected readonly AuctionManagementService Manager = new AuctionManagementService();
        protected readonly DataGridView Grid;
        protected readonly Button RefreshButton;
        protected AuctionFormBase(string title, int actor, PermissionFlags flags)
        {
            Actor = actor;
            Flags = flags;
            Text = title;
            Width = 1080;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Grid = Theme.Grid(20, 80, 1020, 330);
            Grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(Grid);
            RefreshButton = Theme.Button("Tải lại", 20, 18, 130);
            Controls.Add(RefreshButton);
            RefreshButton.Click += (s, e) => Reload();
            Shown += (s, e) => Reload();
        }
        protected abstract void Reload(); //Khai báo hợp đồng/điểm mở rộng; lớp triển khai cung cấp xử lý
        protected TextBox Field(string text, int x, int y, int width = 180) //Tạo trường nhập dùng chung.
        {
            Controls.Add(Theme.Label(text, x, y, width));
            var box = Theme.Input(x, y + 27, width);
            Controls.Add(box);
            return box;
        }
        protected Button Action(string title, int x, int y, bool enabled, Action callback) //Tạo/gắn nút thao tác và quyền
        {
            var b = Theme.Button(title, x, y, 170);
            b.Enabled = enabled;
            Controls.Add(b);
            b.Click += (s, e) => Theme.Safe(() => //Safe: Bắt lỗi thao tác giao diện và thông báo.; Reload: Tải lại dữ liệu và cập nhật lưới
            {
                callback(); Reload();
            });
            return b;
        }
        protected static int SelectedId(DataGridView grid, string key) //Đọc khóa từ lựa chọn trên lưới
        {
            if (grid.CurrentRow == null)
            {
                throw new BusinessException("Hãy chọn một dòng.");
            }
            return Convert.ToInt32(grid.CurrentRow.Cells[key].Value);
        }
        protected static int PositiveInt(string s) //Kiểm tra số nguyên dương.
        {
            int n;
            if (!int.TryParse(s, out n) || n <= 0)
            {
                throw new BusinessException("Cần nhập số nguyên dương.");
            }
            return n;
        }
        protected static decimal Money(string s) //Đọc và kiểm tra số tiền nhập
        {
            decimal n;
            if (!decimal.TryParse(s, out n) || n < 0)
            {
                throw new BusinessException("Giá tiền không hợp lệ.");
            }
            return n;
        }
    }
}
