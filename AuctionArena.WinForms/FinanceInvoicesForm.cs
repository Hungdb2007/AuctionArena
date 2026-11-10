using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class FinanceInvoicesForm : FinanceFormBase
    {
        readonly TextBox search, amount;
        readonly ComboBox method, statusFilter;
        readonly DateTimePicker dateFrom, dateTo;
        readonly CheckBox debt;
        readonly DataGridView details, history;
        readonly Label paymentWarning = new Label
        {
            Left = 20, Top = 658, Width = 1040, Height = 27, 
            ForeColor = Color.FromArgb(137, 83, 11), AutoEllipsis = true
        };
        public FinanceInvoicesForm(int actor, PermissionFlags flags) : base("Hóa đơn và thanh toán", actor, flags)
        {
            search = Theme.Input(170, 28, 260);
            Controls.Add(search);
            Controls.Add(Theme.Label("Tìm mã hoặc khách", 170, 0, 250));
            debt = new CheckBox
            {
                Text = "Chỉ còn nợ", Left = 460, Top = 30, Width = 130, ForeColor = Theme.Text
            };
            Controls.Add(debt);
            statusFilter = new ComboBox
            {
                Left = 610, Top = 28, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList
            };
            statusFilter.Items.AddRange(new object[]
            {
                "Tất cả trạng thái", "Unpaid", "PartiallyPaid", "Paid", "Overdue", "Cancelled"
            });
            statusFilter.SelectedIndex = 0;
            Controls.Add(statusFilter);
            dateFrom = new DateTimePicker
            {
                Left = 20, Top = 66, Width = 180, ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short
            };
            dateTo = new DateTimePicker
            {
                Left = 220, Top = 66, Width = 180, ShowCheckBox = true, Checked = false, Format = DateTimePickerFormat.Short
            };
            Controls.Add(dateFrom);
            Controls.Add(dateTo);
            var resetFilter = Theme.Button("Xóa lọc", 835, 22, 130);
            Controls.Add(resetFilter);
            resetFilter.Click+=(s, e) => 
            {
                search.Clear();
                debt.Checked = false;
                statusFilter.SelectedIndex = 0;
                dateFrom.Checked = false;
                dateTo.Checked = false;
                Reload();
            };
            search.TextChanged+=(s, e) => Reload();
            debt.CheckedChanged+=(s, e) => Reload();
            statusFilter.SelectedIndexChanged+=(s, e) => Reload();
            dateFrom.ValueChanged+=(s, e) => Reload();
            dateTo.ValueChanged+=(s, e) => Reload();
            paymentWarning.Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top;
            Controls.Add(paymentWarning);
            Grid.Top = 110;
            Grid.Height = 235;
            details = Theme.Grid(20, 385, 515, 130);
            history = Theme.Grid(550, 385, 530, 130);
            Controls.Add(details);
            Controls.Add(history);
            Grid.SelectionChanged+=(s, e) => ShowSelected();
            Controls.Add(Theme.Label("Giá búa các lot", 20, 352, 300));
            Controls.Add(Theme.Label("Các khoản thu / chuyển cọc", 550, 352, 300));
            amount = Theme.Input(20, 565, 220);
            Controls.Add(Theme.Label("Số tiền thu", 20, 536, 200));
            Controls.Add(amount);
            method = new ComboBox
            {
                Left = 260, Top = 565, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList
            };
            method.Items.AddRange(new object[]
            {
                "Cash", "BankTransfer"
            });
            method.SelectedIndex = 0;
            Controls.Add(method);
            AddButton("Thu thanh toán", 475, 560, flags.Edit, () => 
            {
                decimal money; if (!decimal.TryParse(amount.Text, out money) || money<=0)throw new BusinessException("Số tiền thu phải lớn hơn 0.");
                var row = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as FinanceInvoiceRow;
                if (row == null)
                {
                    throw new BusinessException("Hãy chọn hóa đơn.");
                }
                if (money>row.Remaining)
                {
                    throw new BusinessException("Số thu vượt số tiền còn nợ.");
                }
                string reference = null;
                if (method.Text == "BankTransfer")
                {
                    reference = Theme.AskReason(this, "Mã xác nhận chuyển khoản ngân hàng (5–100 ký tự):");
                    if (reference == null)
                    {
                        return;
                    }
                }
                var confirmedReference = reference;
                Execute(() => new BillingService().PayInvoice(Actor, Id("InvoiceId"), money, method.SelectedItem.ToString(), confirmedReference), 
                "Xác nhận thu "+money.ToString("N0")+" đ cho "+row.InvoiceCode+
                "?\nCông nợ trước khi thu: "+row.Remaining.ToString("N0")+" đ.");
            });
            AddButton("Tạo hóa đơn", 680, 560, flags.Create, () => 
            {
                using (var dialog = new FinanceCreateInvoiceForm(Actor))dialog.ShowDialog(this);
                Reload();
            });
            AddButton("In hóa đơn", 870, 560, flags.Print, () => ReportForm.OpenInvoice(this, Actor, Id("InvoiceId")));
            var receipt = AddButton("In phiếu thu", 20, 703, flags.Print, () => 
            {
                var payment = history.CurrentRow == null?null:history.CurrentRow.DataBoundItem as FinancePaymentRow;
                if (payment == null)
                {
                    throw new BusinessException("Hãy chọn khoản thu trong bảng lịch sử trước khi in.");
                }
                if (payment.PaymentType != "Invoice")throw new BusinessException("Phiếu này chỉ in thu thanh toán; chuyển cọc là bút toán nội bộ.");
                ReportForm.OpenPaymentReceipt(this, Actor, payment.PaymentId);
            });
            receipt.Width = 185;
            var cancel = AddButton("Hủy hóa đơn chưa thu", 220, 703, flags.Edit, () => 
            {
                var selected = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as FinanceInvoiceRow;
                if (selected == null)
                {
                    throw new BusinessException("Hãy chọn hóa đơn.");
                }
                var reason = Theme.AskReason(this, "Căn cứ hủy hóa đơn chưa thu tiền/chưa chuyển cọc:");
                if (reason == null)
                {
                    return;
                }
                Execute(() => new BillingService().CancelUncollected(Actor, selected.InvoiceId, reason), 
                "Hủy "+selected.InvoiceCode+"? Giữ nguyên dữ liệu chứng từ gốc, không đảo cọc.");
            });
            cancel.Width = 230;
            Height = 840;
            MinimumSize = new Size(1080, 780);
            var overdue = AddButton("Ghi nhận quá hạn", 20, 615, flags.Edit, () => 
            {
                Execute(() => new SessionService().ForfeitOverdueDeposit(Actor, Id("InvoiceId")), 
                "Hóa đơn phải thực sự quá hạn. Ghi nhận vi phạm và tịch thu CHỈ PHẦN CỌC CÒN LẠI " +
                "(không tịch thu lại cọc đã chuyển vào hóa đơn). Tiếp tục?");
            });
            overdue.Width = 205;
            // Xử lý bỏ cọc thay đổi cả đăng ký và phiên; không chỉ dựa quyền Hóa đơn.
            var rights = new AccountService().Permissions(Actor);
            PermissionFlags sessionsRight, registrationRight;
            bool canReauction = flags.Edit && 
            rights.TryGetValue("SESSIONS", out sessionsRight) && sessionsRight.Edit && 
            rights.TryGetValue("REGISTRATIONS", out registrationRight) && registrationRight.Edit;
            var reauction = AddButton("Bỏ cọc / lên phiên mới", 250, 615, canReauction, () => 
            {
                if (Grid.CurrentRow == null)
                {
                    throw new BusinessException("Hãy chọn hóa đơn quá hạn.");
                }
                var row = Grid.CurrentRow.DataBoundItem as FinanceInvoiceRow;
                if (row == null || row.Status != "Overdue")
                {
                    throw new BusinessException("Hóa đơn phải được ghi nhận quá hạn trước.");
                }
                var resolution = new OverdueResolutionService();
                var preview = resolution.Preview(Actor, row.InvoiceId);
                if (MessageBox.Show("HỦY hóa đơn: "+preview.InvoiceCode+
                "\nSố lot đưa về kho: "+preview.LotCount+
                "\nTổng giá búa: "+preview.HammerTotal.ToString("N0")+" đ"+
                "\nCọc đã chuyển hóa đơn cần đảo/tịch thu: "+preview.AppliedDeposit.ToString("N0")+" đ"+
                "\nCọc còn lại cần tịch thu: "+preview.AdditionalForfeiture.ToString("N0")+" đ"+
                "\nCọc đã tịch thu trước đó: "+preview.AlreadyForfeited.ToString("N0")+" đ"+
                "\nTỔNG TỊCH THU THAO TÁC NÀY: "+preview.TotalForfeiture.ToString("N0")+" đ"+
                "\n\nKhông xóa chứng từ gốc; lot cũ được đánh dấu bỏ cọc."+
                "\nXác nhận thực hiện?", 
                "Xác nhận đối soát trước khi hủy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
                int count = resolution.CancelUnpaidOverdueForReauction(Actor, row.InvoiceId);
                MessageBox.Show("Đã xử lý "+count+" lot, chứng từ cũ được bảo toàn."); Reload();
            });
            reauction.Width = 240;
            if (!canReauction)
            new ToolTip().SetToolTip(reauction, "Yêu cầu quyền sửa Hóa đơn, Phiên và Đăng ký tham gia.");
            // Trường hợp khách đến thanh toán sau hạn, nhưng chưa có quyết định tịch thu cọc.
            // Phải có quyền quản lý phiên và nhập rõ căn cứ phê duyệt trước khi ghi sổ.
            bool canLate = flags.Edit && rights.TryGetValue("SESSIONS", out sessionsRight) && sessionsRight.Edit;
            var late = AddButton("Thu trễ có phê duyệt", 520, 615, canLate, () => 
            {
                var row = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as FinanceInvoiceRow;
                if (row == null)
                {
                    throw new BusinessException("Hãy chọn hóa đơn cần thu.");
                }
                if (row.DueAt>=DateTime.Now || (row.Status != "Unpaid" && row.Status != "PartiallyPaid"))
                throw new BusinessException("Chỉ xử lý hóa đơn đã quá hạn và chưa ghi nhận tịch thu cọc.");
                decimal money;
                if (!decimal.TryParse(amount.Text, out money) || money<=0m || money>row.Remaining)
                throw new BusinessException("Số tiền thu phải hợp lệ và không vượt công nợ còn lại.");
                var reason = AskLateApproval();
                if (reason == null)
                {
                    return;
                }
                if (MessageBox.Show("Xác nhận khoản thu trễ " + money.ToString("N0") + " đ"+
                " cho hóa đơn " + row.InvoiceCode +
                "?\nHạn gốc: " + row.DueAt.ToString("dd/MM/yyyy HH:mm") +
                "\nLý do/phê duyệt: " + reason +
                "\nKhông tự xóa vi phạm hoặc hoàn cọc đã tịch thu.", 
                "Kiểm tra trước khi ghi sổ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
                new BillingService().PayLateInvoiceWithApproval(Actor, row.InvoiceId, money, method.Text, reason);
                Reload();
                MessageBox.Show("Đã ghi khoản thu trễ và nhật ký. Hãy đối chiếu sổ quỹ thực tế.");
            });
            late.Width = 205;
            var review = AddButton("Mở biên bản đối soát", 750, 615, flags.Edit, () => 
            {
                var row = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as FinanceInvoiceRow;
                if (row == null || row.DueAt>=DateTime.Now || row.Remaining<=0m || row.PaidAmount<=0m)
                throw new BusinessException("Hãy chọn hóa đơn quá hạn đã thu một phần.");
                var reason = AskLateApproval();
                if (reason == null)
                {
                    return;
                }
                if (MessageBox.Show("Lưu biên bản đối soát công nợ "+row.Remaining.ToString("N0")+
                " đ?\nKhông tịch thu cọc hoặc điều chỉnh hóa đơn tự động.", 
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
                var id = new InvoiceReviewService().OpenPartialOverdueReview(Actor, row.InvoiceId, reason);
                MessageBox.Show("Đã lập hồ sơ đối soát #"+id+". Thu trễ chỉ thực hiện khi được phê duyệt.");
            });
            review.Width = 225;
            if (!canLate)
            new ToolTip().SetToolTip(late, "Yêu cầu quyền sửa Hóa đơn và Quản lý phiên.");
        }
        private string AskLateApproval()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Căn cứ phê duyệt thu trễ";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.Width = 610;
                dialog.Height = 245;
                Theme.Apply(dialog);
                var label = Theme.Label("Ghi mã biên bản, người phê duyệt và lý do (10–180 ký tự):", 16, 14, 560);
                var note = new TextBox
                {
                    Left = 16, Top = 50, Width = 550, Height = 90, Multiline = true, 
                    ScrollBars = ScrollBars.Vertical, MaxLength = 180, BackColor = Theme.Panel, ForeColor = Theme.Text
                };
                var ok = Theme.Button("Xác nhận", 290, 157, 130);
                ok.DialogResult = DialogResult.OK;
                var cancel = Theme.Button("Hủy", 436, 157, 130);
                cancel.DialogResult = DialogResult.Cancel;
                dialog.Controls.Add(label);
                dialog.Controls.Add(note);
                dialog.Controls.Add(ok);
                dialog.Controls.Add(cancel);
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }
                return note.Text.Trim();
            }
        }
        protected override void Reload()
        {
            Theme.Safe(() => 
            {
                Grid.DataSource = Query.Invoices(Actor, search.Text, debt.Checked, 
                statusFilter.SelectedIndex == 0?null:statusFilter.Text, 
                dateFrom.Checked?(DateTime?)dateFrom.Value:null, dateTo.Checked?(DateTime?)dateTo.Value:null); ShowSelected();
            });
        }
        void ShowSelected()
        {
            if (Grid.CurrentRow == null)
            {
                details.DataSource = null;
                history.DataSource = null;
                amount.Clear();
                paymentWarning.Text = "";
                return;
            }
            Theme.Safe(() => 
            {
                int id = Id("InvoiceId"); details.DataSource = Query.InvoiceLots(Actor, id);
                history.DataSource = Query.InvoicePayments(Actor, id);
                var row = Grid.CurrentRow.DataBoundItem as FinanceInvoiceRow;
                if (row != null)
                {
                    amount.Text = row.Remaining>0?row.Remaining.ToString("0.##"):"0";
                    paymentWarning.Text = row.Status == "Cancelled" ?
                    "Hóa đơn đã hủy: giữ chứng từ để truy vết, không thu hoặc giao hàng." :
                    row.Remaining>0 && row.DueAt<DateTime.Now && row.PaidAmount>0 ?
                    "CẢNH BÁO: Hóa đơn đã quá hạn và có khoản thu thực tế. Kế toán phải đối soát, không xử lý bỏ cọc tự động." :
                    row.Remaining>0 && row.DueAt<DateTime.Now ?
                    "Hóa đơn đã quá hạn: dừng thu tiền tự động, kiểm tra điều kiện ghi nhận vi phạm." : "";
                }
            });
        }
    }
}
