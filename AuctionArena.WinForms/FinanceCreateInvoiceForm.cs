using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class FinanceCreateInvoiceForm : Form
    {
        readonly FinanceQueryService query = new FinanceQueryService();
        readonly ComboBox sessions, customers;
        public FinanceCreateInvoiceForm(int actor)
        {
            Text = "Lập hóa đơn gộp theo khách và phiên";
            Width = 540;
            Height = 315;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            Controls.Add(Theme.Label("Chọn phiên đã kết thúc", 20, 24, 420));
            sessions = new ComboBox
            {
                Left = 20, Top = 54, Width = 470, DropDownStyle = ComboBoxStyle.DropDownList, 
                DisplayMember = "Name", ValueMember = "Id"
            };
            Controls.Add(sessions);
            Controls.Add(Theme.Label("Khách thắng lot chưa lập hóa đơn", 20, 107, 470));
            customers = new ComboBox
            {
                Left = 20, Top = 137, Width = 470, DropDownStyle = ComboBoxStyle.DropDownList, 
                DisplayMember = "Name", ValueMember = "Id"
            };
            Controls.Add(customers);
            var create = Theme.Button("Lập hóa đơn", 20, 199, 215);
            Controls.Add(create);
            sessions.SelectedIndexChanged+=(s, e) => Theme.Safe(() => 
            {
                var session = sessions.SelectedItem as InvoiceSessionOption;
                customers.DataSource = session == null?null:query.InvoiceCustomers(actor, session.Id);
            });
            Shown+=(s, e) => Theme.Safe(() => sessions.DataSource = query.InvoiceSessions(actor));
            create.Click+=(s, e) => Theme.Safe(() => 
            {
                var session = sessions.SelectedItem as InvoiceSessionOption;
                var customer = customers.SelectedItem as InvoiceCustomerOption;
                if (session == null || customer == null)
                throw new BusinessException("Hãy chọn phiên đã kết thúc và khách có lot thắng chưa lập hóa đơn.");
                int id = new BillingService().CreateInvoice(actor, session.Id, customer.Id);
                MessageBox.Show("Đã lập hóa đơn số " + id); DialogResult = DialogResult.OK; Close();
            });
        }
    }
    // Giao từng phần theo danh sách hiện vật, mỗi phiếu là một lần bàn giao thực tế.
}
