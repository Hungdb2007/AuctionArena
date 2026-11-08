using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class ConsignorsForm : CatalogFormBase<ConsignorEntity>
    {
        readonly CatalogService service = new CatalogService();
        readonly TextBox code, name, phone, identity, address, bank, bankAccount, fee, email, representative, accountHolder, notes;
        readonly ComboBox type, filterType, filterStatus;
        public ConsignorsForm(int actor, PermissionFlags flags):base("Chủ hàng ký gửi", actor, flags)
        {
            code = Field("Mã chủ hàng", 18, 416);
            name = Field("Họ tên", 230, 416);
            phone = Field("Số điện thoại", 442, 416);
            identity = Field("Định danh", 654, 416);
            address = Field("Địa chỉ", 18, 505, 650);
            bank = Field("Ngân hàng nhận tiền", 18, 591, 250);
            bankAccount = Field("Số tài khoản", 290, 591, 320);
            fee = Field("Phí chủ hàng (%) (để trống dùng phí chung)", 18, 677, 310);
            type = Combo("Loại chủ hàng", 360, 677, 185);
            type.Items.AddRange(new object[]
            {
                "Individual", "Organization"
            });
            type.SelectedIndex = 0;
            email = Field("Email", 565, 677, 325);
            representative = Field("Người đại diện", 18, 758, 300);
            accountHolder = Field("Chủ tài khoản ngân hàng", 340, 758, 325);
            notes = Field("Ghi chú chủ hàng", 18, 845, 650);
            filterType = new ComboBox
            {
                Left = 18, Top = 948, Width = 195, DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterType.Items.AddRange(new object[]
            {
                "Tất cả loại", "Individual", "Organization"
            });
            filterType.SelectedIndex = 0;
            Controls.Add(filterType);
            filterStatus = new ComboBox
            {
                Left = 235, Top = 948, Width = 195, DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterStatus.Items.AddRange(new object[]
            {
                "Tất cả trạng thái", "Active", "Inactive"
            });
            filterStatus.SelectedIndex = 0;
            Controls.Add(filterStatus);
            var filter = Theme.Button("Áp dụng lọc", 448, 944, 165);
            Controls.Add(filter);
            filter.Click+=(s, e) => Reload();
            var clear = Theme.Button("Xóa lọc", 626, 944, 150);
            Controls.Add(clear);
            clear.Click+=(s, e) => 
            {
                filterType.SelectedIndex = 0;
                filterStatus.SelectedIndex = 0;
                Reload();
            };
            var portfolio = Theme.Button("Hiện vật, hợp đồng và công nợ", 18, 1000, 360);
            Controls.Add(portfolio);
            portfolio.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn chủ hàng.");
                }
                var summary = service.ConsignorSummary(Actor, Current.ConsignorId);
                using (var dialog = new Form
                {
                    Text = "Hồ sơ chủ hàng: "+Current.FullName, Width = 980, Height = 680, 
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dialog);
                    string[] labels = 
                    {
                        "Số hiện vật: "+summary.Items, "Số hợp đồng: "+summary.Contracts, 
                        "Đã chi trả: "+summary.Paid.ToString("N0")+" đ", "Còn dự kiến chi: "+summary.Outstanding.ToString("N0")+" đ"
                    };
                    for (int i = 0; i<labels.Length; i++)dialog.Controls.Add(Theme.Label(labels[i], 25, 18+i*31, 850));
                    var tabs = new TabControl
                    {
                        Left = 20, Top = 170, Width = 915, Height = 430
                    };
                    var assets = new TabPage("Hiện vật & Hợp đồng");
                    var assetsGrid = Theme.Grid(8, 8, 875, 190); assetsGrid.DataSource = service.ConsignorItems(Actor, Current.ConsignorId);
                    var contractGrid = Theme.Grid(8, 204, 875, 177); contractGrid.DataSource = service.ConsignorContracts(Actor, Current.ConsignorId);
                    assets.Controls.Add(assetsGrid); assets.Controls.Add(contractGrid);
                    var payouts = new TabPage("Công nợ & chi trả");
                    var payoutGrid = Theme.Grid(8, 8, 875, 373); payoutGrid.DataSource = service.ConsignorPayouts(Actor, Current.ConsignorId);
                    payouts.Controls.Add(payoutGrid);
                    tabs.TabPages.Add(assets); tabs.TabPages.Add(payouts); dialog.Controls.Add(tabs);
                    dialog.ShowDialog(this);
                }
            });
            MoveActions(1090);
            Height = 1220;
        }
        protected override object Fetch(string q)
        {
            return service.Consignors(Actor, q, 
            filterType.SelectedIndex<=0?null:filterType.Text, 
            filterStatus.SelectedIndex<=0?null:filterStatus.Text);
        }
        protected override void Fill(ConsignorEntity x)
        {
            code.Text = x.ConsignorCode;
            name.Text = x.FullName;
            phone.Text = x.Phone;
            identity.Text = x.IdentityNumber;
            address.Text = x.Address;
            bank.Text = x.BankName;
            bankAccount.Text = x.BankAccount;
            fee.Text = x.SellerFeeRate.HasValue?(x.SellerFeeRate.Value*100m).ToString("0.##"):"";
            email.Text = x.Email;
            type.SelectedItem = x.ConsignorType??"Individual";
            representative.Text = x.RepresentativeName;
            accountHolder.Text = x.BankAccountHolder;
            notes.Text = x.Notes;
        }
        protected override void Clear()
        {
            foreach (var t in new[]
            {
                code, name, phone, identity, address, bank, bankAccount, fee, email, representative, accountHolder, notes
            })t.Clear();
            type.SelectedIndex = 0;
        }
        protected override void Delete()
        {
            service.DeleteConsignor(Actor, Current.ConsignorId);
        }
        protected override void Save()
        {
            service.SaveConsignor(Actor, new ConsignorEntity
            {
                ConsignorId = Current == null?0:Current.ConsignorId, ConsignorCode = code.Text, FullName = name.Text, Phone = phone.Text, IdentityNumber = identity.Text, Address = address.Text, BankName = bank.Text, BankAccount = bankAccount.Text, 
                SellerFeeRate = string.IsNullOrWhiteSpace(fee.Text)?(decimal?)null:Money(fee)/100m, 
                Email = email.Text, RepresentativeName = representative.Text, BankAccountHolder = accountHolder.Text, 
                ConsignorType = type.Text, Notes = notes.Text
            });
        }
    }
}
