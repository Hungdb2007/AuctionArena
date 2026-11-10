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
    public sealed class AuctionSessionsForm : AuctionFormBase
    {
        readonly TextBox code, name;
        readonly DateTimePicker starts, ends;
        public AuctionSessionsForm(int actor, PermissionFlags flags) : base("Phiên đấu giá", actor, flags)
        {
            code = Field("Mã phiên", 20, 435);
            name = Field("Tên phiên", 220, 435, 350);
            starts = new DateTimePicker
            {
                Left = 20, Top = 530, Width = 250, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true
            };
            ends = new DateTimePicker
            {
                Left = 290, Top = 530, Width = 250, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true
            };
            Controls.Add(Theme.Label("Bắt đầu", 20, 500));
            Controls.Add(Theme.Label("Kết thúc", 290, 500));
            Controls.Add(starts);
            Controls.Add(ends);
            ends.Value = DateTime.Now.AddDays(1);
            Height = 790;
            MinimumSize = new Size(1000, 750);
            Action("Tạo phiên", 600, 475, flags.Create, () => 
            {
                Manager.SaveSession(Actor, 0, code.Text, name.Text, starts.Value, ends.Value); MessageBox.Show("Đã tạo phiên.");
            });
            Action("Duyệt lịch phiên", 800, 475, flags.Edit, () => Manager.ScheduleSession(Actor, SelectedId(Grid, "SessionId")));
            Action("Sửa phiên", 600, 530, flags.Edit, () => Manager.SaveSession(Actor, SelectedId(Grid, "SessionId"), code.Text, name.Text, starts.Value, ends.Value));
            Action("Mở phiên", 820, 640, flags.Edit, () => Manager.OpenSession(Actor, SelectedId(Grid, "SessionId")));
            Action("Kết thúc phiên", 800, 530, flags.Edit, () => 
            {
                if (MessageBox.Show("Kết thúc phiên đã chọn?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                new SessionService().CloseSession(Actor, SelectedId(Grid, "SessionId"));
            });
            var clone = Action("Nhân bản phiên nháp", 600, 640, flags.Create, () => 
            {
                int id = SelectedId(Grid, "SessionId");
                var newCode = code.Text.Trim()+"-COPY";
                if (newCode.Length>30)
                {
                    throw new BusinessException("Mã phiên nhân bản vượt 30 ký tự. Rút ngắn mã trước.");
                }
                Manager.CloneSessionDraft(Actor, id, newCode, name.Text.Trim()+" (bản sao)", starts.Value.AddDays(7), ends.Value.AddDays(7));
            });
            clone.Width = 205;
            Action("Hủy phiên chưa mở", 600, 585, flags.Edit, () => 
            {
                if (MessageBox.Show("Chỉ được hủy phiên nháp chưa có giao dịch. Tiếp tục?", "Xác nhận hủy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                Manager.CancelDraftSession(Actor, SelectedId(Grid, "SessionId"));
            });
            Action("Cọc cần hoàn khi đóng", 800, 585, flags.Edit, () => 
            {
                var id = SelectedId(Grid, "SessionId");
                var preview = new DepositSettlementService().PreviewLoserRefunds(Actor, id);
                MessageBox.Show("Số khách chưa nhận lại tiền cọc: "+preview.CustomerCount+
                "\nSố tiền chờ hoàn: "+preview.TotalAmount.ToString("N0")+" đ.\n"+
                "Hãy mở mục Đăng ký/Thu cọc của phiên để thực chi và xác nhận phiếu hoàn.", 
                "Đối soát cọc cần hoàn");
            });
            Grid.SelectionChanged += (s, e) =>  //Xử lý tương tác và cập nhật trạng thái/hiển thị trong khối sự kiện.
            {
                if (Grid.CurrentRow == null)
                {
                    return;
                }
                var row = Grid.CurrentRow.DataBoundItem as AuctionSessionEntity;
                if (row == null)
                {
                    return;
                }
                code.Text = row.SessionCode;
                name.Text = row.SessionName;
                starts.Value = row.StartsAt;
                ends.Value = row.EndsAt;
            };
        }
        protected override void Reload()
        {
            Theme.Safe(() => Grid.DataSource = Manager.Sessions(Actor, ""));
        }
    }
}
