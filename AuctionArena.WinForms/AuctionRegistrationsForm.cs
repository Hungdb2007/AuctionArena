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
    public sealed class AuctionRegistrationsForm : AuctionFormBase
    {
        readonly int sessionId;
        readonly ComboBox customers, method;
        readonly TextBox paddle, deposit;
        public AuctionRegistrationsForm(int actor, PermissionFlags flags, int session):base("Đăng ký và thu cọc – Phiên "+session, actor, flags)
        {
            sessionId = session;
            customers = new ComboBox
            {
                Left = 20, Top = 469, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "FullName", ValueMember = "CustomerId"
            };
            Controls.Add(Theme.Label("Khách tham gia", 20, 440));
            Controls.Add(customers);
            paddle = Field("Số paddle", 340, 440, 120);
            deposit = Field("Số tiền thu cọc", 480, 440, 185);
            method = new ComboBox
            {
                Left = 680, Top = 469, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList
            };
            method.Items.AddRange(new object[]
            {
                "Cash", "BankTransfer"
            });
            method.SelectedIndex = 0;
            Controls.Add(Theme.Label("Phương thức", 680, 440));
            Controls.Add(method);
            Action("Đăng ký khách", 20, 535, flags.Create, () => new RegistrationService().Register(Actor, sessionId, (int)customers.SelectedValue, PositiveInt(paddle.Text)));
            Action("Thu tiền cọc", 220, 535, flags.Edit, () => new DepositService().Collect(Actor, SelectedId(Grid, "RegistrationId"), Money(deposit.Text), method.Text));
            Action("Hoàn tiền cọc", 420, 535, flags.Edit, () => 
            {
                if (MessageBox.Show("Đã hoàn tiền cho khách bằng tiền mặt và muốn ghi nhận giao dịch?", 
                "Xác nhận thực chi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                new DepositService().Refund(Actor, SelectedId(Grid, "RegistrationId"));
            });
            var batch = Action("Hoàn cọc khách không thắng", 20, 590, flags.Edit, () => 
            {
                var service = new DepositSettlementService();
                var preview = service.PreviewLoserRefunds(Actor, sessionId);
                if (preview.CustomerCount == 0)
                {
                    throw new BusinessException("Không có khoản cọc cần hoàn.");
                }
                string question = "Phiên này có " + preview.CustomerCount + " khách không thắng cần hoàn " +
                preview.TotalAmount.ToString("N0") + " đ.\n\n" +
                "Chỉ nhấn Yes khi bạn ĐÃ CHI TIỀN MẶT cho các khách này. " +
                "Hệ thống sẽ ghi nhận toàn bộ phiếu hoàn trong một giao dịch. Tiếp tục?";
                if (MessageBox.Show(question, "Xác nhận đã hoàn cọc", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                var done = service.RefundLosersByCash(Actor, sessionId);
                MessageBox.Show("Đã ghi nhận hoàn " + done.TotalAmount.ToString("N0") +
                " đ cho " + done.CustomerCount + " khách.", "Hoàn cọc thành công");
            });
            batch.Width = 275;
        }
        protected override void Reload() //Tải lại dữ liệu và cập nhật lưới
        {
            Theme.Safe(() => 
            {
                Grid.DataSource = Manager.Registrations(Actor, sessionId); customers.DataSource = Manager.EligibleCustomers(Actor);
            });
        }
    }
    // Bàn điều hành cho nhân viên sàn, khách giơ thẻ paddle và không đăng nhập.
}
