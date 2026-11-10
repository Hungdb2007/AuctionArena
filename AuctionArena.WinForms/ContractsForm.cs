using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class ContractsForm : Form
    {
        readonly int actor;
        readonly ConsignmentService service = new ConsignmentService();
        readonly CatalogService catalog = new CatalogService();
        readonly ComboBox owner = new ComboBox
        {
            Left = 20, Top = 53, Width = 425, DropDownStyle = ComboBoxStyle.DropDownList
        };
        readonly Label hint = Theme.Label("Chọn chủ hàng để xem các hiện vật đủ điều kiện", 20, 94, 950);
        readonly DataGridView choices = Theme.Grid(20, 126, 1000, 315);
        readonly DataGridView contracts = Theme.Grid(20, 745, 1000, 175);
        readonly TextBox notes = Theme.Input(20, 573, 510), fee = Theme.Input(557, 573, 155);
        readonly DateTimePicker expiry = new DateTimePicker
        {
            Left = 740, Top = 573, Width = 230, Format = DateTimePickerFormat.Short
        };
        readonly DateTimePicker signedDate = new DateTimePicker
        {
            Left = 20, Top = 633, Width = 220, Format = DateTimePickerFormat.Short
        };
        System.ComponentModel.BindingList<ContractItemRow> pending = new System.ComponentModel.BindingList<ContractItemRow>();
        public ContractsForm(int actor, PermissionFlags flags)
        {
            this.actor = actor;
            Text = "Hợp đồng ký gửi và phiếu nhập kho";
            Width = 1080;
            Height = 970;
            MinimumSize = new Size(960, 700);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Chọn chủ hàng ký gửi", 20, 22, 350));
            Controls.Add(owner);
            Controls.Add(hint);
            Controls.Add(choices);
            Controls.Add(contracts);
            SetupCandidateGrid();
            var select = Theme.Button("Chọn tất cả", 20, 451, 170);
            Controls.Add(select);
            select.Click+=(s, e) => ToggleAll(true);
            var clear = Theme.Button("Bỏ chọn", 210, 451, 150);
            Controls.Add(clear);
            clear.Click+=(s, e) => ToggleAll(false);
            var details = Theme.Button("Xem chi tiết hiện vật", 380, 451, 230);
            Controls.Add(details);
            details.Click+=(s, e) => Theme.Safe(() => 
            {
                var selected = choices.CurrentRow == null?null:choices.CurrentRow.DataBoundItem as ContractItemRow;
                if (selected == null)
                {
                    throw new BusinessException("Chọn một hiện vật để xem chi tiết.");
                }
                MessageBox.Show("Mã: "+selected.ItemCode+"\nTên: "+selected.ItemName+
                "\nGiá ước tính: "+selected.EstimatePrice.ToString("N0")+" đ"+
                "\nTình trạng: "+selected.Condition+"\nVị trí: "+selected.Location+
                "\nNguồn gốc: "+selected.Origin, "Chi tiết hiện vật");
            });
            var reload = Theme.Button("Tải lại hiện vật", 630, 451, 180);
            Controls.Add(reload);
            reload.Click+=(s, e) => Theme.Safe(LoadItems);
            Controls.Add(Theme.Label("Ghi chú hợp đồng", 20, 542, 300));
            Controls.Add(notes);
            Controls.Add(Theme.Label("Phí bán thỏa thuận (%)", 557, 542, 170));
            Controls.Add(fee);
            Controls.Add(Theme.Label("Ngày hết hạn", 740, 542, 220));
            Controls.Add(expiry);
            expiry.Value = DateTime.Today.AddMonths(6);
            signedDate.Value = DateTime.Today;
            Controls.Add(Theme.Label("Ngày ký", 20, 604, 205));
            Controls.Add(signedDate);
            var save = Theme.Button("Ký hợp đồng (chưa nhập kho)", 260, 631, 315);
            save.Enabled = flags.Create;
            Controls.Add(save);
            save.Click+=(s, e) => Theme.Safe(() => SaveContract());
            var cancelContract = Theme.Button("Hủy hợp đồng trước nhập kho", 755, 699, 265);
            cancelContract.Enabled = flags.Edit;
            Controls.Add(cancelContract);
            cancelContract.Click+=(s, e) => Theme.Safe(CancelSelectedContract);
            var print = Theme.Button("In hợp đồng đã chọn", 590, 631, 240);
            print.Enabled = flags.Print;
            Controls.Add(print);
            print.Click+=(s, e) => Theme.Safe(() => 
            {
                var selected = contracts.CurrentRow == null?null:contracts.CurrentRow.DataBoundItem as ContractSummary;
                if (selected == null)
                {
                    throw new BusinessException("Hãy chọn hợp đồng cần in.");
                }
                ReportForm.OpenContract(this, actor, selected.ContractId);
            });
            var extend = Theme.Button("Gia hạn hợp đồng", 840, 631, 180);
            extend.Enabled = flags.Edit;
            Controls.Add(extend);
            extend.Click+=(s, e) => Theme.Safe(() => 
            {
                var selected = contracts.CurrentRow == null?null:contracts.CurrentRow.DataBoundItem as ContractSummary;
                if (selected == null)
                {
                    throw new BusinessException("Hãy chọn hợp đồng ở bảng phía dưới.");
                }
                if (MessageBox.Show("Gia hạn hợp đồng "+selected.Code+" đến "+expiry.Value.ToString("dd/MM/yyyy")+"?\n"+
                "Không thay đổi mức phí đã ký.", "Xác nhận gia hạn", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)return;
                service.ExtendContract(actor, selected.ContractId, expiry.Value.Date);
                ReloadContracts(); MessageBox.Show("Gia hạn hợp đồng thành công.");
            });
            Controls.Add(Theme.Label("Các hợp đồng đã lập", 20, 708, 300));
            owner.SelectedIndexChanged+=(s, e) => Theme.Safe(LoadItems);
            Shown+=(s, e) => Theme.Safe(() => 
            {
                owner.ValueMember = "ConsignorId"; owner.DisplayMember = "FullName";
                owner.DataSource = catalog.ConsignorOptions(actor, "CONTRACTS");
                LoadItems(); ReloadContracts();
            });
        }
        void SetupCandidateGrid()
        {
            choices.ReadOnly = false;
            choices.AutoGenerateColumns = false;
            choices.Columns.Clear();
            choices.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected", HeaderText = "Chọn", Width = 65
            });
            choices.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemCode", HeaderText = "Mã", Width = 130, ReadOnly = true
            });
            choices.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemName", HeaderText = "Hiện vật", Width = 310, ReadOnly = true
            });
            choices.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "EstimatePrice", HeaderText = "Giá ước tính", Width = 170, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N0"
                }
            });
            choices.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Condition", HeaderText = "Tình trạng", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true
            });
            choices.CellContentClick+=(s, e) => 
            {
                if (e.RowIndex>=0 && e.ColumnIndex == 0)choices.EndEdit();
            };
            choices.CellDoubleClick+=(s, e) => 
            {
                if (e.RowIndex>=0 && e.ColumnIndex != 0)
                {
                    var row = choices.Rows[e.RowIndex].DataBoundItem as ContractItemRow;
                    if (row != null)
                    {
                        row.Selected = !row.Selected;
                        choices.Refresh();
                    }
                }
            };
        }
        void ToggleAll(bool select)
        {
            choices.EndEdit();
            foreach (var x in pending)x.Selected = select;
            pending.ResetBindings();
        }
        void LoadItems()
        {
            if (!(owner.SelectedValue is int))
            {
                return;
            }
            var rows = catalog.PendingItemsForContract(actor, (int)owner.SelectedValue)
            .Select(x => new ContractItemRow
            {
                ItemId = x.ItemId, ItemCode = x.ItemCode, ItemName = x.ItemName, 
                EstimatePrice = x.EstimatePrice, Condition = x.ConditionDescription, 
                Location = x.WarehouseLocation, Origin = x.Origin
            }).ToList();
            pending = new System.ComponentModel.BindingList<ContractItemRow>(rows);
            choices.DataSource = pending;
            hint.Text = rows.Count == 0?"Chủ hàng chưa có hiện vật chờ ký gửi. Hãy tạo hiện vật và thẩm định trước.":
            "Có "+rows.Count+" hiện vật đang chờ ký gửi. Tick chọn rồi lập hợp đồng.";
        }
        void ReloadContracts()
        {
            contracts.DataSource = service.Contracts(actor);
        }
        void CancelSelectedContract()
        {
            var selected = contracts.CurrentRow == null?null:contracts.CurrentRow.DataBoundItem as ContractSummary;
            if (selected == null)
            {
                throw new BusinessException("Hãy chọn một hợp đồng trong danh sách.");
            }
            if (selected.Status != "Active")
            {
                throw new BusinessException("Chỉ được hủy hợp đồng còn hiệu lực.");
            }
            var reason = Theme.AskReason(this, "Lý do hủy hợp đồng trước khi nhập kho:");
            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }
            if (MessageBox.Show("Hủy hợp đồng "+selected.Code+"? Thao tác không xóa chứng từ.", 
            "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
            service.CancelContractBeforeReceipt(actor, selected.ContractId, reason);
            ReloadContracts();
            LoadItems();
        }
        void SaveContract()
        {
            choices.EndEdit();
            var ids = pending.Where(x => x.Selected).Select(x => x.ItemId).ToList();
            if (ids.Count == 0)
            {
                throw new BusinessException("Phải chọn ít nhất một hiện vật.");
            }
            var consignor = owner.SelectedValue is int?(int)owner.SelectedValue:0;
            if (consignor<=0)
            {
                throw new BusinessException("Chưa chọn chủ hàng.");
            }
            decimal percentage;
            decimal? requestedRate = null;
            if (!string.IsNullOrWhiteSpace(fee.Text))
            {
                if (!decimal.TryParse(fee.Text, out percentage) || percentage<0m || percentage>100m)
                throw new BusinessException("Vui lòng nhập tỷ lệ phí từ 0 đến 100%.");
                requestedRate = percentage/100m;
            }
            string feeText = requestedRate.HasValue?(requestedRate.Value*100m).ToString("0.##")+"%":
            "theo tỷ lệ chủ hàng/cấu hình hiện hành";
            if (MessageBox.Show("Ký hợp đồng "+ids.Count+" hiện vật, phí bán "+feeText+"?\nLưu ý: chưa tự động nhập kho.", 
            "Lập hợp đồng", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)return;
            var id = service.ReceiveContract(actor, consignor, ids, notes.Text, requestedRate, expiry.Value.Date, signedDate.Value.Date);
            MessageBox.Show("Đã ký hợp đồng #"+id+". Hiện vật vẫn CHƯA nhập kho.\nHãy mở mục Phiếu nhập kho để thủ kho kiểm tra và xác nhận nhận hàng.");
            notes.Clear();
            LoadItems();
            ReloadContracts();
        }
        public sealed class ContractItemRow
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
            public decimal EstimatePrice
            {
                get;
                set;
            }
            public string Condition
            {
                get;
                set;
            }
            public string Origin
            {
                get;
                set;
            }
            public string Location
            {
                get;
                set;
            }
        }
    }
    // Phiếu nhập được thủ kho xác nhận sau khi hợp đồng ký gửi đã lập.
}
