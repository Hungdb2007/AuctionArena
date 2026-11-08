using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class FinancePayoutsForm : FinanceFormBase
    {
        readonly DataGridView eligible;
        readonly TextBox sellerFeePercent = Theme.Input(20, 610, 150);
        readonly ComboBox payoutMethod = new ComboBox
        {
            Left = 450, Top = 688, Width = 225, 
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        public FinancePayoutsForm(int actor, PermissionFlags flags) : base("Chi trả chủ hàng", actor, flags)
        {
            Height = 850;
            AutoScroll = true;
            Grid.Height = 210;
            Controls.Add(Theme.Label("Các khoản đã chi trả", 20, 65, 350));
            Controls.Add(Theme.Label("Hình thức chi thực tế", 450, 657, 230));
            payoutMethod.Items.AddRange(new object[]
            {
                "Cash", "BankTransfer"
            });
            payoutMethod.SelectedIndex = 0;
            Controls.Add(payoutMethod);
            Controls.Add(Theme.Label("Các lot đủ điều kiện chi trả", 20, 332, 350));
            eligible = Theme.Grid(20, 365, 1060, 190);
            eligible.ReadOnly = false;
            eligible.DataBindingComplete+=(s, e) => 
            {
                foreach (DataGridViewColumn c in eligible.Columns)c.ReadOnly = c.Name != "BulkChoose";
            };
            eligible.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "BulkChoose", HeaderText = "Chọn", Width = 65
            });
            Controls.Add(eligible);
            var batch = AddButton("Lập nhiều lệnh chờ chi", 500, 574, flags.Create, () => 
            {
                eligible.EndEdit();
                var ids = eligible.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && 
                r.Cells["BulkChoose"].Value != null && Convert.ToBoolean(r.Cells["BulkChoose"].Value))
                .Select(r => (r.DataBoundItem as FinanceLotRow)).Where(x => x != null).Select(x => x.LotId).ToList();
                if (ids.Count == 0)
                {
                    throw new BusinessException("Tick chọn các lot cần lập bảng kê.");
                }
                Execute(() => new SettlementService().PreparePayoutBatch(Actor, ids), 
                "Tạo "+ids.Count+" lệnh CHỜ CHI cùng giao dịch? Chưa ghi nhận đã trả tiền.");
            });
            batch.Width = 260;
            var prepare = AddButton("Lập lệnh chờ chi", 805, 574, flags.Create, () => 
            {
                var row = eligible.CurrentRow == null?null:eligible.CurrentRow.DataBoundItem as FinanceLotRow;
                if (row == null)
                {
                    throw new BusinessException("Chọn một lot đủ điều kiện để lập lệnh chi.");
                }
                if (row.PayoutOrderStatus != "Chưa lập lệnh")
                throw new BusinessException("Lot đã có lệnh chi.");
                if (!row.PayoutAmount.HasValue)
                {
                    throw new BusinessException("Chưa chốt phí người bán.");
                }
                Execute(() => new SettlementService().PrepareSellerPayout(Actor, row.LotId), 
                "Lập lệnh CHỜ CHI cho "+row.Consignor+" – "+row.PayoutAmount.Value.ToString("N0")+
                " đ?\nThao tác này không ghi nhận đã chuyển tiền.");
            });
            prepare.Width = 225;
            AddButton("Chi trả lot đã chọn", 20, 574, flags.Create, () => 
            {
                if (eligible.CurrentRow == null)
                {
                    throw new BusinessException("Hãy chọn lot đủ điều kiện.");
                }
                int id = Convert.ToInt32(eligible.CurrentRow.Cells["LotId"].Value);
                var row = eligible.CurrentRow.DataBoundItem as FinanceLotRow;
                if (row == null)
                {
                    throw new BusinessException("Chưa chọn lot.");
                }
                if (!row.SellerFeeRate.HasValue)
                {
                    throw new BusinessException("Lot chưa chốt phí bán, hãy xác nhận phí trước.");
                }
                if (row.PayoutOrderStatus != "Chờ chi")
                {
                    throw new BusinessException("Cần lập lệnh chờ chi trước khi xác nhận đã trả tiền.");
                }
                var transfer = payoutMethod.Text == "BankTransfer";
                if (transfer && (string.IsNullOrWhiteSpace(row.BankName) || string.IsNullOrWhiteSpace(row.BankAccount)))
                throw new BusinessException("Chủ hàng chưa có đủ thông tin ngân hàng và số tài khoản; vui lòng sửa hồ sơ chủ hàng.");
                string bankText = transfer ? "\nNgân hàng: "+row.BankName+"\nSố tài khoản: "+row.BankAccount : "";
                string reference = null;
                if (transfer)
                {
                    reference = Theme.AskReason(this, "Mã giao dịch đã chuyển khoản (5–100 ký tự):"); if (reference == null)return;
                }
                Execute(() => new SettlementService().PaySeller(Actor, id, payoutMethod.Text, reference), 
                "Chủ hàng: "+row.Consignor+"\nGiá búa: "+row.HammerPrice.ToString("N0")+
                " đ\nPhí bán: "+row.SellerFee.Value.ToString("N0")+
                " đ\nTHỰC CHI: "+row.PayoutAmount.Value.ToString("N0")+
                " đ\nHình thức chi: "+payoutMethod.Text+bankText+
                "\n\nCHỈ xác nhận sau khi đã giao tiền hoặc hoàn tất chuyển khoản thực tế.");
            });
            sellerFeePercent.Top = 687;
            Controls.Add(Theme.Label("Phí cho lot cũ chưa chốt (%)", 20, 655, 300));
            Controls.Add(sellerFeePercent);
            var cancelOrder = AddButton("Hủy lệnh chờ chi", 705, 684, flags.Edit, () => 
            {
                var row = eligible.CurrentRow == null?null:eligible.CurrentRow.DataBoundItem as FinanceLotRow;
                if (row == null || row.PayoutOrderStatus != "Chờ chi")
                {
                    throw new BusinessException("Chọn lot có lệnh chờ chi.");
                }
                var reason = Theme.AskReason(this, "Lý do hủy lệnh chưa chi (5–200 ký tự):");
                if (reason == null)
                {
                    return;
                }
                Execute(() => new SettlementService().CancelPendingPayoutOrder(Actor, row.LotId, reason), 
                "Hủy lệnh chưa chi cho lot "+row.LotId+"? Không xóa lịch sử.");
            });
            cancelOrder.Width = 220;
            var reopenOrder = AddButton("Mở lại lệnh đã hủy", 450, 751, flags.Edit, () => 
            {
                var row = eligible.CurrentRow == null?null:eligible.CurrentRow.DataBoundItem as FinanceLotRow;
                if (row == null || row.PayoutOrderStatus != "Đã hủy")
                {
                    throw new BusinessException("Chọn lot có lệnh Đã hủy.");
                }
                var reason = Theme.AskReason(this, "Căn cứ phê duyệt mở lại lệnh chưa chi (10–200 ký tự):");
                if (reason == null)
                {
                    return;
                }
                Execute(() => new SettlementService().ReopenCancelledPayoutOrder(Actor, row.LotId, reason), 
                "Mở lại lệnh đã hủy cho lot "+row.LotId+"? Không ghi nhận tiền.");
            });
            reopenOrder.Width = 220;
            var payoutReceipt = AddButton("In phiếu chi", 20, 751, flags.Print, () => 
            {
                if (Grid.CurrentRow == null)
                {
                    throw new BusinessException("Chọn chứng từ đã chi trong bảng phía trên.");
                }
                int id = Id("PayoutId");
                ReportForm.OpenSellerPayoutReceipt(this, Actor, id);
            });
            payoutReceipt.Width = 185;
            Height = 900;
            MinimumSize = new Size(1050, 820);
            var confirm = Theme.Button("Chốt phí lot cũ", 200, 684, 190);
            confirm.Enabled = flags.Edit;
            Controls.Add(confirm);
            confirm.Click+=(s, e) => Theme.Safe(() => 
            {
                if (eligible.CurrentRow == null)
                {
                    throw new BusinessException("Chọn một lot cần xác nhận phí.");
                }
                var selected = eligible.CurrentRow.DataBoundItem as FinanceLotRow;
                if (selected == null || selected.SellerFeeRate.HasValue)
                {
                    throw new BusinessException("Lot này đã có tỷ lệ phí.");
                }
                decimal rate;
                if (!decimal.TryParse(sellerFeePercent.Text, out rate) || rate<0m || rate>100m)
                throw new BusinessException("Nhập phí từ 0% đến 100%.");
                if (MessageBox.Show("Khóa phí "+rate+"% cho lot #"+selected.LotId+"?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                new SettlementService().ConfirmLegacySellerFee(Actor, selected.LotId, rate/100m); Reload();
            });
        }
        protected override void Reload()
        {
            Theme.Safe(() => 
            {
                Grid.DataSource = Query.Payouts(Actor); eligible.DataSource = Query.EligiblePayouts(Actor);
            });
        }
    }
}
