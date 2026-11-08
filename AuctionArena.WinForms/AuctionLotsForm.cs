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
    public sealed class AuctionLotsForm : AuctionFormBase
    {
        readonly int sessionId;
        readonly ComboBox items;
        readonly TextBox number, starting, reserve, step, lotDeposit;
        readonly DateTimePicker starts, ends;
        public AuctionLotsForm(int actor, PermissionFlags flags, int session) : base("Thiết lập lot – Phiên " + session, actor, flags)
        {
            sessionId = session;
            items = new ComboBox
            {
                Left = 20, Top = 469, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "ItemName", ValueMember = "ItemId"
            };
            Controls.Add(Theme.Label("Hiện vật trong kho", 20, 440));
            Controls.Add(items);
            number = Field("Số lot", 305, 440, 100);
            starting = Field("Giá khởi điểm", 420, 440, 170);
            reserve = Field("Giá sàn", 610, 440, 165);
            step = Field("Bước giá", 795, 440, 165);
            lotDeposit = Field("Tiền cọc lot (để trống tính theo tỷ lệ chung)", 20, 630, 370);
            Height = 790;
            MinimumSize = new Size(1000, 750);
            AutoScroll = true;
            starts = new DateTimePicker
            {
                Left = 20, Top = 560, Width = 250, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true
            };
            ends = new DateTimePicker
            {
                Left = 300, Top = 560, Width = 250, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true
            };
            ends.Value = DateTime.Now.AddHours(1);
            Controls.Add(Theme.Label("Mở lot", 20, 531));
            Controls.Add(Theme.Label("Đóng lot", 300, 531));
            Controls.Add(starts);
            Controls.Add(ends);
            Action("Thêm lot", 610, 553, flags.Create, () => 
            {
                if (items.SelectedValue == null)
                {
                    throw new BusinessException("Chưa chọn hiện vật.");
                }
                Manager.CreateLot(Actor, sessionId, (int)items.SelectedValue, PositiveInt(number.Text), Money(starting.Text), Money(reserve.Text), Money(step.Text), starts.Value, ends.Value, 
                string.IsNullOrWhiteSpace(lotDeposit.Text)?(decimal?)null:Money(lotDeposit.Text));
            });
            Action("Mở lot", 810, 553, flags.Edit, () => Manager.OpenLot(Actor, SelectedId(Grid, "LotId")));
            var batch = Action("Thêm nhiều lot", 400, 690, flags.Create, () => CreateBatch());
            batch.Width = 185;
            var renumber = Action("Đánh lại số lot", 610, 690, flags.Edit, () => 
            {
                if (MessageBox.Show("Đánh lại số lot liên tục từ 1?", "Xác nhận", MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question) != DialogResult.Yes)return;
                Manager.RenumberPlannedLots(Actor, sessionId);
            });
            renumber.Width = 205;
        }
        void CreateBatch() //Thu thập lựa chọn và tạo nhiều lô(67-125).
        {
            var available = Manager.AvailableItems(Actor);
            if (available.Count == 0)
            {
                throw new BusinessException("Không còn hiện vật trong kho để xếp lot.");
            }
            using (var dlg = new Form
            {
                Text = "Chọn hiện vật để tạo nhiều lot", Width = 650, Height = 600, 
                StartPosition = FormStartPosition.CenterParent
            })
            {
                Theme.Apply(dlg);
                var checks = new CheckedListBox
                {
                    Left = 20, Top = 20, Width = 575, Height = 385, 
                    CheckOnClick = true, BackColor = Theme.Panel, ForeColor = Theme.Text
                };
                foreach (var item in available)checks.Items.Add(new LotItemChoice
                {
                    Id = item.ItemId, 
                    Label = item.ItemCode+" – "+item.ItemName
                });
                dlg.Controls.Add(checks);
                dlg.Controls.Add(Theme.Label("Số lot đầu tiên", 20, 420, 220));
                var first = Theme.Input(20, 450, 170);
                first.Text = "1";
                dlg.Controls.Add(first);
                dlg.Controls.Add(Theme.Label("Giá và thời gian lấy từ Form lot chính.", 215, 420, 365));
                var apply = Theme.Button("Tạo các lot đã chọn", 20, 505, 245);
                dlg.Controls.Add(apply);

                //Safe: Bắt lỗi thao tác giao diện và thông báo.; PositiveInt: Kiểm tra số nguyên dương.; Money: Đọc và kiểm tra số tiền nhập.
                apply.Click+=(s, e) => Theme.Safe(() => 
                {
                    var checkedItems = checks.CheckedItems.Cast<LotItemChoice>().ToList();
                    if (checkedItems.Count == 0)
                    {
                        throw new BusinessException("Chưa chọn hiện vật.");
                    }
                    int begin = PositiveInt(first.Text);
                    if (begin>int.MaxValue-checkedItems.Count)
                    {
                        throw new BusinessException("Số lot vượt giới hạn.");
                    }
                    decimal start = Money(starting.Text), floor = Money(reserve.Text), inc = Money(step.Text);
                    decimal? dep = string.IsNullOrWhiteSpace(lotDeposit.Text)?(decimal?)null:Money(lotDeposit.Text);
                    var rows = checkedItems.Select((x, i) => new AuctionManagementService.NewLotRow
                    {
                        ItemId = x.Id, LotNumber = begin+i, StartingPrice = start, ReservePrice = floor, 
                        BidIncrement = inc, DepositRequired = dep, OpensAt = starts.Value, ClosesAt = ends.Value
                    }).ToList();
                    if (MessageBox.Show("Lập "+rows.Count+" lot? Một dòng lỗi sẽ hủy toàn bộ.", 
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)return;
                    Manager.CreateLotsBatch(Actor, sessionId, rows);
                    dlg.DialogResult = DialogResult.OK; dlg.Close();
                });
                dlg.ShowDialog(this);
            }
        }
        sealed class LotItemChoice
        {
            public int Id
            {
                get;
                set;
            }
            public string Label
            {
                get;
                set;
            }
            public override string ToString() //Cung cấp nhãn hiển thị cho đối tượng lựa chọn.
            {
                return Label;
            }
        }
        protected override void Reload() //Tải lại dữ liệu và cập nhật lưới.
        {
            Theme.Safe(() => 
            {
                Grid.DataSource = Manager.Lots(Actor, sessionId);
                items.DataSource = Manager.AvailableItems(Actor);
            });
        }
    }
}
