using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class StockReceiptsForm : Form
    {
        readonly ConsignmentService service = new ConsignmentService();
        readonly int actor;
        readonly ComboBox contracts = new ComboBox
        {
            Left = 18, Top = 60, Width = 650, DropDownStyle = ComboBoxStyle.DropDownList, 
            DisplayMember = "Display", ValueMember = "ContractId"
        };
        readonly DataGridView pending = Theme.Grid(18, 115, 970, 225), receipts = Theme.Grid(18, 535, 970, 145), details = Theme.Grid(18, 725, 970, 180);
        readonly DateTimePicker received = new DateTimePicker
        {
            Left = 690, Top = 60, Width = 290, Format = DateTimePickerFormat.Short
        };
        readonly TextBox notes = Theme.Input(18, 377, 550);
        readonly TextBox search = Theme.Input(505, 486, 320);
        readonly TextBox deliveredBy = Theme.Input(580, 377, 260);
        public StockReceiptsForm(int actor, PermissionFlags flags)
        {
            this.actor = actor;
            Text = "Phiếu nhập kho – xác nhận nhận hiện vật";
            Width = 1050;
            Height = 940;
            MinimumSize = new Size(1010, 780);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Hợp đồng ký gửi đang chờ nhận hàng", 18, 25, 460));
            Controls.Add(contracts);
            Controls.Add(Theme.Label("Ngày nhận thực tế", 690, 25, 275));
            Controls.Add(received);
            Controls.Add(Theme.Label("Đánh dấu hiện vật đã kiểm tra; nhập tình trạng và vị trí thực tế", 18, 88, 840));
            Controls.Add(pending);
            SetupPendingGrid();
            Controls.Add(Theme.Label("Ghi chú biên bản nhận hàng", 18, 349, 450));
            Controls.Add(notes);
            Controls.Add(Theme.Label("Người giao hiện vật", 580, 349, 260));
            Controls.Add(deliveredBy);
            var save = Theme.Button("Xác nhận nhập kho", 590, 410, 220);
            save.Enabled = flags.Create;
            Controls.Add(save);
            save.Click+=(s, e) => Theme.Safe(() => Confirm());
            var refreshPending = Theme.Button("Tải lại hợp đồng", 820, 410, 170);
            Controls.Add(refreshPending);
            refreshPending.Click+=(s, e) => Theme.Safe(LoadContracts);
            Controls.Add(Theme.Label("Phiếu nhập kho đã lập", 18, 450, 370));
            Controls.Add(search);
            var refresh = Theme.Button("Tìm / tải lại", 840, 482, 155);
            Controls.Add(refresh);
            Controls.Add(receipts);
            Controls.Add(Theme.Label("Chi tiết phiếu nhập đã chọn", 18, 698, 420));
            Controls.Add(details);
            contracts.SelectedIndexChanged+=(s, e) => Theme.Safe(LoadPending);
            refresh.Click+=(s, e) => Theme.Safe(LoadReceipts);
            receipts.SelectionChanged+=(s, e) => Theme.Safe(() => 
            {
                var selected = receipts.CurrentRow == null?null:receipts.CurrentRow.DataBoundItem as ReceiptSummary;
                details.DataSource = selected == null?null:service.ReceiptItems(actor, selected.ReceiptId);
            });
            Shown+=(s, e) => Theme.Safe(() => 
            {
                LoadContracts(); LoadReceipts();
            });
        }
        void SetupPendingGrid()
        {
            pending.AutoGenerateColumns = false;
            pending.ReadOnly = false;
            pending.Columns.Clear();
            pending.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Selected", HeaderText = "Đã nhận", Width = 75
            });
            pending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemCode", HeaderText = "Mã hiện vật", Width = 135, ReadOnly = true
            });
            pending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemName", HeaderText = "Tên hiện vật", Width = 245, ReadOnly = true
            });
            pending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Condition", HeaderText = "Tình trạng lúc nhận", Width = 275, MaxInputLength = 400
            });
            pending.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Location", HeaderText = "Vị trí bảo quản", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MaxInputLength = 100
            });
            pending.CellContentClick+=(s, e) => 
            {
                if (e.RowIndex>=0 && e.ColumnIndex == 0)pending.EndEdit();
            };
        }
        void LoadContracts()
        {
            contracts.DataSource = service.ContractsAwaitingReceipt(actor);
            LoadPending();
        }
        void LoadPending()
        {
            var selection = contracts.SelectedItem as ReceiptContractOption;
            pending.DataSource = selection == null?new System.ComponentModel.BindingList<StockReceiveGridLine>():
            new System.ComponentModel.BindingList<StockReceiveGridLine>(service.AwaitingReceiptItems(actor, selection.ContractId)
            .Select(x => new StockReceiveGridLine
            {
                ItemId = x.ItemId, ItemCode = x.ItemCode, ItemName = x.ItemName, 
                Condition = x.ConditionAtReceipt, Location = x.LocationAtReceipt
            }).ToList());
        }
        void LoadReceipts()
        {
            receipts.DataSource = service.Receipts(actor, search.Text.Trim());
        }
        void Confirm()
        {
            pending.EndEdit();
            var contract = contracts.SelectedItem as ReceiptContractOption;
            if (contract == null)
            {
                throw new BusinessException("Chưa có hợp đồng chờ nhận hàng.");
            }
            var list = pending.DataSource as System.ComponentModel.BindingList<StockReceiveGridLine>;
            var lines = (list??new System.ComponentModel.BindingList<StockReceiveGridLine>())
            .Where(x => x.Selected).Select(x => new ReceiptLine
            {
                ItemId = x.ItemId, 
                ConditionAtReceipt = x.Condition, LocationAtReceipt = x.Location
            }).ToList();
            if (lines.Count == 0)
            {
                throw new BusinessException("Hãy tick hiện vật đã nhận thực tế.");
            }
            if (MessageBox.Show("Xác nhận thủ kho đã nhận thực tế "+lines.Count+" hiện vật của "+contract.Display+
            "?\nSau xác nhận các hiện vật này sẽ chuyển sang Trong kho.", 
            "Xác nhận nhận hàng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
            var id = service.ConfirmStockReceipt(actor, contract.ContractId, lines, notes.Text, received.Value.Date, deliveredBy.Text);
            MessageBox.Show("Đã lập phiếu nhập #"+id+" và cập nhật trạng thái kho.");
            notes.Clear();
            LoadContracts();
            LoadReceipts();
        }
        sealed class StockReceiveGridLine
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
            public string Condition
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
}
