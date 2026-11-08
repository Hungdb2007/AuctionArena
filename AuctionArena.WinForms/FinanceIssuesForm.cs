using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class FinanceIssuesForm : FinanceFormBase
    {
        readonly DataGridView pending, items, drafts;
        readonly TextBox recipient, document, relation;
        readonly ComboBox deliveryMethod;
        readonly System.ComponentModel.BindingList<DeliveryChoice> selected = 
        new System.ComponentModel.BindingList<DeliveryChoice>();
        public FinanceIssuesForm(int actor, PermissionFlags flags):base("Xuất kho và bàn giao", actor, flags)
        {
            Height = 1180;
            MinimumSize = new Size(1080, 900);
            AutoScroll = true;
            Grid.Height = 170;
            Controls.Add(Theme.Label("Các phiếu xuất đã lập", 20, 64, 400));
            Controls.Add(Theme.Label("Hóa đơn đã thanh toán đủ, còn hiện vật chờ giao", 20, 282, 600));
            pending = Theme.Grid(20, 312, 1060, 135);
            Controls.Add(pending);
            Controls.Add(Theme.Label("Chọn hiện vật và ghi tình trạng khi giao (nhập ở cột thứ tư)", 20, 457, 850));
            items = Theme.Grid(20, 488, 1060, 185);
            items.ReadOnly = false;
            items.AutoGenerateColumns = false;
            items.Columns.Clear();
            items.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = "Giao", DataPropertyName = "Selected", Width = 70
            });
            items.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Mã", DataPropertyName = "ItemCode", ReadOnly = true, Width = 105
            });
            items.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Hiện vật", DataPropertyName = "ItemName", ReadOnly = true, Width = 330
            });
            items.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Tình trạng khi giao", DataPropertyName = "Condition", Width = 360
            });
            items.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Vị trí kho", DataPropertyName = "WarehouseLocation", ReadOnly = true, Width = 135
            });
            items.DataSource = selected;
            Controls.Add(items);
            pending.SelectionChanged+=(s, e) => LoadItems();
            Controls.Add(Theme.Label("Người nhận", 20, 690, 230));
            recipient = Theme.Input(20, 717, 240);
            Controls.Add(recipient);
            Controls.Add(Theme.Label("CCCD / hộ chiếu người nhận", 277, 690, 255));
            document = Theme.Input(277, 717, 240);
            Controls.Add(document);
            Controls.Add(Theme.Label("Quan hệ / giấy ủy quyền", 535, 690, 250));
            relation = Theme.Input(535, 717, 245);
            Controls.Add(relation);
            Controls.Add(Theme.Label("Hình thức giao", 797, 690, 240));
            deliveryMethod = new ComboBox
            {
                Left = 797, Top = 717, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList
            };
            deliveryMethod.Items.AddRange(new object[]
            {
                "Tại sàn", "Giao tận nơi"
            });
            deliveryMethod.SelectedIndex = 0;
            Controls.Add(deliveryMethod);
            var deliver = AddButton("Xác nhận giao đã chọn", 20, 777, flags.Create, () => 
            {
                if (pending.CurrentRow == null)
                {
                    throw new BusinessException("Hãy chọn hóa đơn.");
                }
                if (!items.EndEdit())
                {
                    throw new BusinessException("Tình trạng hiện vật không hợp lệ.");
                }
                var lines = selected.Where(x => x.Selected).Select(x => new SettlementService.DeliveryLine
                {
                    ItemId = x.ItemId, Condition = x.Condition
                }).ToList();
                if (lines.Count == 0)
                {
                    throw new BusinessException("Chọn ít nhất một hiện vật để giao.");
                }
                int invoice = Convert.ToInt32(pending.CurrentRow.Cells["InvoiceId"].Value);
                string mode = deliveryMethod.SelectedIndex == 1?"Delivery":"AtVenue";
                Execute(() => new SettlementService().IssueItems(Actor, invoice, recipient.Text, lines, 
                document.Text, relation.Text, mode), 
                "Đã bàn giao đúng "+lines.Count+" hiện vật cho "+recipient.Text+"? Chỉ nhấn Có sau khi thực giao.");
            });
            deliver.Width = 230;
            var print = AddButton("In phiếu xuất", 275, 777, flags.Print, () => ReportForm.OpenIssue(this, Actor, Id("IssueId")));
            print.Width = 185;
            var saveDraft = AddButton("Lưu phiếu nháp", 485, 777, flags.Create, () => 
            {
                if (pending.CurrentRow == null)
                {
                    throw new BusinessException("Hãy chọn hóa đơn cần giao.");
                }
                if (!items.EndEdit())
                {
                    throw new BusinessException("Tình trạng bàn giao chưa hợp lệ.");
                }
                var lines = selected.Where(x => x.Selected).Select(x => new SettlementService.DeliveryLine
                {
                    ItemId = x.ItemId, Condition = x.Condition
                }).ToList();
                if (lines.Count == 0)
                {
                    throw new BusinessException("Chọn hiện vật để lưu nháp.");
                }
                int invoice = Convert.ToInt32(pending.CurrentRow.Cells["InvoiceId"].Value);
                int id = new SettlementService().SaveIssueDraft(Actor, invoice, recipient.Text, lines, 
                document.Text, relation.Text, deliveryMethod.SelectedIndex == 1?"Delivery":"AtVenue");
                MessageBox.Show("Đã lưu phiếu nháp số "+id+", CHƯA xuất kho."); Reload();
            });
            saveDraft.Width = 190;
            Controls.Add(Theme.Label("Phiếu xuất kho nháp – chưa giảm tồn", 20, 852, 600));
            drafts = Theme.Grid(20, 885, 1060, 165);
            Controls.Add(drafts);
            var confirmDraft = AddButton("Xác nhận nháp đã giao", 20, 1065, flags.Create, () => 
            {
                if (drafts.CurrentRow == null)
                {
                    throw new BusinessException("Chọn phiếu nháp cần xác nhận.");
                }
                int id = Convert.ToInt32(drafts.CurrentRow.Cells["IssueId"].Value);
                Execute(() => new SettlementService().ConfirmIssueDraft(Actor, id), 
                "Đã kiểm tra hồ sơ người nhận và BÀN GIAO THỰC TẾ toàn bộ hiện vật trong phiếu nháp? "+
                "Khi xác nhận, kho sẽ được cập nhật.");
            });
            confirmDraft.Width = 230;
            var cancelDraft = AddButton("Hủy nháp", 270, 1065, flags.Edit, () => 
            {
                if (drafts.CurrentRow == null)
                {
                    throw new BusinessException("Chọn phiếu nháp cần hủy.");
                }
                int id = Convert.ToInt32(drafts.CurrentRow.Cells["IssueId"].Value);
                var reason = Theme.AskReason(this, "Lý do hủy nháp (5–200 ký tự):");
                if (reason == null)
                {
                    return;
                }
                Execute(() => new SettlementService().CancelIssueDraft(Actor, id, reason), 
                "Hủy phiếu nháp "+id+"? Phiếu chưa giao và không giảm kho.");
            });
            cancelDraft.Width = 185;
        }
        void LoadItems()
        {
            selected.Clear();
            var row = pending.CurrentRow == null?null:pending.CurrentRow.DataBoundItem as FinanceInvoiceRow;
            if (row == null)
            {
                return;
            }
            foreach (var it in Query.PendingIssueItems(Actor, row.InvoiceId))
            selected.Add(new DeliveryChoice
            {
                ItemId = it.ItemId, ItemCode = it.ItemCode, 
                ItemName = it.ItemName, WarehouseLocation = it.WarehouseLocation, Condition = ""
            });
        }
        protected override void Reload()
        {
            Theme.Safe(() => 
            {
                Grid.DataSource = Query.Issues(Actor); pending.DataSource = Query.ReadyToIssue(Actor);
                drafts.DataSource = Query.IssueDrafts(Actor); LoadItems();
            });
        }
        sealed class DeliveryChoice
        {
            public bool Selected
            {
                get;
                set;
            }
            public int ItemId
            {
                get;
                set;
            }
            public string ItemCode
            {
                get;
                set;
            }
            public string ItemName
            {
                get;
                set;
            }
            public string WarehouseLocation
            {
                get;
                set;
            }
            public string Condition
            {
                get;
                set;
            }
        }
    }
}
