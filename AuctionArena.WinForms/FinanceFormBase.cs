using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    // Các thao tác ghi gọi Service để kiểm tra quyền và xử lý transaction.
    public abstract class FinanceFormBase : Form
    {
        protected readonly int Actor;
        protected readonly PermissionFlags Flags;
        protected readonly FinanceQueryService Query = new FinanceQueryService();
        protected readonly DataGridView Grid;
        protected FinanceFormBase(string title, int actor, PermissionFlags flags)
        {
            Actor = actor;
            Flags = flags;
            Text = title;
            Width = 1120;
            Height = 730;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Grid = Theme.Grid(20, 90, 1060, 360);
            Grid.Anchor = AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            Controls.Add(Grid);
            var refresh = Theme.Button("Tải lại", 20, 18, 130);
            Controls.Add(refresh);
            refresh.Click+=(s, e) => Reload();
            Shown+=(s, e) => Reload();
        }
        protected abstract void Reload();
        protected int Id(string column)
        {
            if (Grid.CurrentRow == null)
            {
                throw new BusinessException("Hãy chọn một dòng trước.");
            }
            return Convert.ToInt32(Grid.CurrentRow.Cells[column].Value);
        }
        protected void Execute(Action work, string question)
        {
            if (MessageBox.Show(question, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            Theme.Safe(() => 
            {
                work(); Reload(); MessageBox.Show("Thao tác thành công.", "Thông báo");
            });
        }
        protected Button AddButton(string text, int x, int y, bool allowed, Action action)
        {
            var button = Theme.Button(text, x, y, 170);
            button.Enabled = allowed;
            Controls.Add(button);
            button.Click+=(s, e) => Theme.Safe(action);
            return button;
        }
    }
}
