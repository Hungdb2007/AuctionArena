using System;
using System.Drawing;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class InventoryForm : Form
    {
        readonly int actor;
        readonly WarehouseWorkflowService service = new WarehouseWorkflowService();
        readonly DataGridView grid = Theme.Grid(20, 115, 1000, 440);
        readonly TextBox search = Theme.Input(20, 61, 320);
        readonly ComboBox status = new ComboBox
        {
            Left = 365, Top = 61, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList
        };
        public InventoryForm(int actor)
        {
            this.actor = actor;
            Text = "Tồn kho hiện vật";
            Width = 1070;
            Height = 660;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Tìm mã, hiện vật hoặc chủ hàng", 20, 30, 320));
            Controls.Add(search);
            Controls.Add(Theme.Label("Trạng thái", 365, 30, 200));
            Controls.Add(status);
            status.Items.AddRange(new object[]
            {
                "Tất cả", "Trong kho", "Đang đấu giá", "Đã bán chờ giao", "Không bán được"
            });
            status.SelectedIndex = 0;
            var reload = Theme.Button("Tìm / tải lại", 620, 58, 170);
            Controls.Add(reload);
            reload.Click+=(s, e) => Reload();
            status.SelectedIndexChanged+=(s, e) => Reload();
            grid.Anchor = AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right|AnchorStyles.Bottom;
            Controls.Add(grid);
            Shown+=(s, e) => Reload();
        }
        void Reload()
        {
            Theme.Safe(() => 
            {
                string[] codes = 
                {
                    "", "InStock", "InAuction", "Sold", "Unsold"
                };
                grid.DataSource = service.Inventory(actor, search.Text, codes[Math.Max(0, status.SelectedIndex)]);
            });
        }
    }
    public sealed class ConsignorReturnsForm : Form
    {
        readonly int actor;
        readonly PermissionFlags flags;
        readonly WarehouseWorkflowService service = new WarehouseWorkflowService();
        readonly DataGridView candidates = Theme.Grid(20, 75, 995, 250), completed = Theme.Grid(20, 465, 995, 140);
        readonly TextBox recipient = Theme.Input(20, 367, 300), notes = Theme.Input(340, 367, 330);
        public ConsignorReturnsForm(int actor, PermissionFlags flags)
        {
            this.actor = actor;
            this.flags = flags;
            Text = "Biên bản trả hiện vật không bán được";
            Width = 1060;
            Height = 720;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Hiện vật đủ điều kiện trả chủ hàng", 20, 35, 500));
            Controls.Add(candidates);
            Controls.Add(Theme.Label("Người nhận / đại diện chủ hàng", 20, 335, 300));
            Controls.Add(recipient);
            Controls.Add(Theme.Label("Ghi chú tình trạng khi giao", 340, 335, 330));
            Controls.Add(notes);
            var apply = Theme.Button("Xác nhận đã bàn giao", 700, 364, 230);
            apply.Enabled = flags.Create;
            Controls.Add(apply);
            apply.Click+=(s, e) => Theme.Safe(() => 
            {
                if (candidates.CurrentRow == null)
                {
                    throw new BusinessException("Chọn hiện vật muốn trả.");
                }
                var choice = candidates.CurrentRow.DataBoundItem as ReturnCandidateRow;
                if (choice == null)
                {
                    throw new BusinessException("Không có hiện vật đã chọn.");
                }
                if (MessageBox.Show("Đã bàn giao hiện vật "+choice.ItemCode+" cho "+recipient.Text+"?", "Xác nhận giao thực tế", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
                var id = service.ReturnToConsignor(actor, choice.LotId, recipient.Text, notes.Text);
                MessageBox.Show("Đã tạo biên bản trả số " + id); Reload();
            });
            var requeue = Theme.Button("Đưa lại vào kho để đấu giá", 450, 630, 300);
            Controls.Add(requeue);
            requeue.Click+=(s, e) => Theme.Safe(() => 
            {
                if (candidates.CurrentRow == null)
                {
                    throw new BusinessException("Chọn hiện vật không đạt giá sàn.");
                }
                var row = candidates.CurrentRow.DataBoundItem as ReturnCandidateRow;
                if (row == null)
                {
                    throw new BusinessException("Không có hiện vật được chọn.");
                }
                if (MessageBox.Show("Đưa hiện vật "+row.ItemCode+" về trạng thái trong kho để xếp phiên khác?", 
                "Đấu giá lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)return;
                service.RequeueUnsold(actor, row.LotId); Reload();
            });
            var print = Theme.Button("In biên bản trả hàng", 770, 630, 210);
            print.Enabled = flags.Print;
            Controls.Add(print);
            print.Click+=(s, e) => Theme.Safe(() => 
            {
                var selected = completed.CurrentRow == null?null:completed.CurrentRow.DataBoundItem as ReturnRecordRow;
                if (selected == null)
                {
                    throw new BusinessException("Hãy chọn biên bản đã lập để in.");
                }
                ReportForm.OpenConsignorReturn(this, actor, selected.ReturnId);
            });
            Controls.Add(Theme.Label("Lịch sử trả hiện vật", 20, 430, 400));
            Controls.Add(completed);
            var refresh = Theme.Button("Tải lại", 820, 28, 160);
            Controls.Add(refresh);
            refresh.Click+=(s, e) => Reload();
            Shown+=(s, e) => Reload();
        }
        void Reload()
        {
            Theme.Safe(() => 
            {
                candidates.DataSource = service.Candidates(actor); completed.DataSource = service.Returns(actor);
            });
        }
    }
}
