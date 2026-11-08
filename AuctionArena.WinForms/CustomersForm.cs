using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class CustomersForm : CatalogFormBase<CustomerEntity>
    {
        readonly CatalogService service = new CatalogService();
        readonly TextBox code, name, phone, email, identity, address, photo;
        readonly ComboBox tier, gender, statusFilter, tierFilter;
        readonly CheckBox black;
        readonly DateTimePicker dob;
        readonly TextBox notes, blackReason;
        byte[] selectedPhoto;
        readonly PictureBox preview = new PictureBox
        {
            Left = 740, Top = 600, Width = 190, Height = 125, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Theme.Panel
        };
        public CustomersForm(int actor, PermissionFlags flags, string presetSearch = null):base("Khách hàng", actor, flags)
        {
            code = Field("Mã khách", 18, 416);
            name = Field("Họ tên", 230, 416);
            phone = Field("Số điện thoại", 442, 416);
            email = Field("Email", 654, 416);
            identity = Field("Giấy tờ định danh", 18, 505);
            tier = Combo("Hạng thành viên", 230, 505);
            tier.Items.AddRange(new object[]
            {
                "Normal", "Silver", "Gold", "VIP"
            });
            tier.SelectedIndex = 0;
            black = new CheckBox
            {
                Text = "Danh sách đen (chỉ xem)", Left = 445, Top = 537, Width = 230, Enabled = false
            };
            Controls.Add(black);
            address = Field("Địa chỉ liên hệ", 18, 590, 620);
            photo = Field("Đường dẫn ảnh khách (tùy chọn)", 18, 674, 620);
            var browse = Theme.Button("Chọn ảnh", 670, 700, 150);
            Controls.Add(browse);
            browse.Enabled = flags.Edit;
            browse.Click+=(s, e) => Theme.Safe(() => 
            {
                using (var dialog = new OpenFileDialog
                {
                    Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
                })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                    if (new System.IO.FileInfo(dialog.FileName).Length>2*1024*1024)
                    throw new BusinessException("Ảnh khách hàng tối đa 2 MB.");
                    selectedPhoto = System.IO.File.ReadAllBytes(dialog.FileName);
                    using (var ms = new System.IO.MemoryStream(selectedPhoto))using (var check = Image.FromStream(ms))
                    {
                        if (check.Width<1 || check.Height<1)
                        {
                            throw new BusinessException("Ảnh không hợp lệ.");
                        }
                    }
                    photo.Text = dialog.FileName;
                }
            });
            Controls.Add(preview);
            ReadOnlyAlways(photo);
            photo.TextChanged+=(s, e) => 
            {
                if (preview.Image != null)
                {
                    var previous = preview.Image;
                    preview.Image = null;
                    previous.Dispose();
                }
                try
                {
                    if (selectedPhoto != null)
                    {
                        using (var ms = new System.IO.MemoryStream(selectedPhoto))
                        using (var img = Image.FromStream(ms))preview.Image = new Bitmap(img);
                    }
                }
                catch
                {
                    // Ảnh không hợp lệ không được làm hỏng hồ sơ khách.
                }
            };
            dob = new DateTimePicker
            {
                Left = 18, Top = 814, Width = 195, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false
            };
            Controls.Add(Theme.Label("Ngày sinh", 18, 785, 200));
            Controls.Add(dob);
            RegisterEditor(dob, "Ngày sinh");
            gender = Combo("Giới tính", 235, 785, 180);
            gender.Items.AddRange(new object[]
            {
                "Male", "Female", "Other"
            });
            gender.SelectedIndex = 0;
            notes = Field("Ghi chú khách", 435, 785, 405);
            blackReason = Field("Lý do blacklist (chỉ xem)", 18, 900, 710);
            ReadOnlyAlways(blackReason);
            tierFilter = new ComboBox
            {
                Left = 18, Top = 1010, Width = 165, DropDownStyle = ComboBoxStyle.DropDownList
            };
            tierFilter.Items.AddRange(new object[]
            {
                "Tất cả hạng", "Normal", "Silver", "Gold", "VIP"
            });
            tierFilter.SelectedIndex = 0;
            Controls.Add(tierFilter);
            statusFilter = new ComboBox
            {
                Left = 200, Top = 1010, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList
            };
            statusFilter.Items.AddRange(new object[]
            {
                "Tất cả trạng thái", "Active", "Blacklist", "Inactive"
            });
            statusFilter.SelectedIndex = 0;
            Controls.Add(statusFilter);
            var apply = Theme.Button("Lọc hồ sơ", 405, 1005, 142);
            Controls.Add(apply);
            apply.Click+=(s, e) => Reload();
            var reset = Theme.Button("Xóa lọc", 560, 1005, 130);
            Controls.Add(reset);
            reset.Click+=(s, e) => 
            {
                tierFilter.SelectedIndex = 0;
                statusFilter.SelectedIndex = 0;
                Reload();
            };
            var blacklistButton = Theme.Button("Đưa vào/gỡ blacklist", 18, 1065, 225);
            blacklistButton.Enabled = flags.Edit;
            Controls.Add(blacklistButton);
            blacklistButton.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Hãy chọn khách hàng trước.");
                }
                using (var dialog = new Form
                {
                    Text = "Thay đổi trạng thái blacklist", Width = 500, Height = 255, StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dialog);
                    dialog.Controls.Add(Theme.Label("Nhập lý do (5–300 ký tự):", 15, 15, 440));
                    var reason = Theme.Input(15, 50, 445); reason.MaxLength = 300; dialog.Controls.Add(reason);
                    var confirm = Theme.Button(black.Checked?"Gỡ blacklist":"Đưa vào blacklist", 15, 115, 205); dialog.Controls.Add(confirm);
                    confirm.Click+=(sender, args) => Theme.Safe(() => 
                    {
                        service.SetBlacklist(Actor, Current.CustomerId, !black.Checked, reason.Text);
                        dialog.DialogResult = DialogResult.OK; dialog.Close();
                    });
                    if (dialog.ShowDialog(this) == DialogResult.OK)Reload();
                }
            });
            var history = Theme.Button("Lịch sử tham gia / hạng", 260, 1065, 240);
            Controls.Add(history);
            history.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Hãy chọn khách hàng.");
                }
                var info = service.CustomerSummary(Actor, Current.CustomerId);
                using (var dialog = new Form
                {
                    Text = "Lịch sử khách hàng: "+Current.FullName, Width = 850, Height = 620, 
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dialog);
                    var tabs = new TabControl
                    {
                        Left = 16, Top = 20, Width = 795, Height = 530
                    };
                    var participate = new TabPage("Tham gia / công nợ");
                    var lines = new[]
                    {
                        "Số phiên đăng ký: "+info.Registrations, "Số lot đã thắng: "+info.WonLots, 
                        "Số hóa đơn: "+info.Invoices, "Tổng đã thanh toán: "+info.TotalSpent.ToString("N0")+" đ", 
                        "Còn nợ: "+info.RemainingDebt.ToString("N0")+" đ", 
                        "Cọc sàn đang giữ: "+info.HeldDeposit.ToString("N0")+" đ"
                    };
                    for (int i = 0; i<lines.Length; i++)participate.Controls.Add(new Label
                    {
                        Left = 25, Top = 30+i*55, Width = 700, 
                        Height = 33, Text = lines[i], ForeColor = Color.Black, Font = new Font("Segoe UI", 12)
                    });
                    var activity = new TabPage("Danh sách phiên và hóa đơn");
                    var activityGrid = Theme.Grid(10, 10, 760, 455);
                    activityGrid.DataSource = service.CustomerActivity(Actor, Current.CustomerId);
                    activity.Controls.Add(activityGrid);
                    var tierHistory = new TabPage("Lịch sử đổi hạng");
                    var grid = Theme.Grid(10, 10, 760, 465); grid.DataSource = service.CustomerTierChanges(Actor, Current.CustomerId);
                    tierHistory.Controls.Add(grid); tabs.TabPages.Add(participate); tabs.TabPages.Add(activity); tabs.TabPages.Add(tierHistory);
                    dialog.Controls.Add(tabs); dialog.ShowDialog(this);
                }
            });
            var removePhoto = Theme.Button("Xóa ảnh đã lưu", 510, 1065, 175);
            removePhoto.Enabled = flags.Edit;
            Controls.Add(removePhoto);
            removePhoto.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn khách hàng.");
                }
                if (MessageBox.Show("Xóa ảnh khách hàng?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                service.RemoveCustomerPhoto(Actor, Current.CustomerId); selectedPhoto = null; Reload();
            });
            MoveActions(1150);
            Height = 1290;
            FormClosed+=(s, e) => 
            {
                if (preview.Image != null)preview.Image.Dispose();
            };
        }
        protected override object Fetch(string q)
        {
            return service.Customers(Actor, q, 
            tierFilter.SelectedIndex<=0?null:tierFilter.Text, 
            statusFilter.SelectedIndex<=0?null:statusFilter.Text);
        }
        protected override void Fill(CustomerEntity x)
        {
            code.Text = x.CustomerCode;
            name.Text = x.FullName;
            phone.Text = x.Phone;
            email.Text = x.Email;
            identity.Text = x.IdentityNumber;
            tier.SelectedItem = x.MemberTier;
            black.Checked = x.IsBlacklisted;
            address.Text = x.Address;
            blackReason.Text = x.BlacklistReason;
            notes.Text = x.Notes;
            gender.SelectedItem = x.Gender??"Other";
            dob.Checked = x.DateOfBirth.HasValue;
            if (x.DateOfBirth.HasValue)dob.Value = x.DateOfBirth.Value;
            selectedPhoto = x.PhotoData;
            photo.Text = x.PhotoData != null?"Ảnh đã lưu trong SQL":(x.PhotoPath??"");
            if (preview.Image != null)
            {
                var old = preview.Image;
                preview.Image = null;
                old.Dispose();
            }
            if (selectedPhoto != null)using (var stream = new System.IO.MemoryStream(selectedPhoto))
            using (var image = Image.FromStream(stream))preview.Image = new Bitmap(image);
        }
        protected override void Clear()
        {
            code.Clear();
            name.Clear();
            phone.Clear();
            email.Clear();
            identity.Clear();
            address.Clear();
            photo.Clear();
            selectedPhoto = null;
            tier.SelectedIndex = 0;
            black.Checked = false;
            notes.Clear();
            blackReason.Clear();
            dob.Checked = false;
            gender.SelectedIndex = 0;
        }
        protected override void Delete()
        {
            service.DeleteCustomer(Actor, Current.CustomerId);
        }
        protected override void Save()
        {
            service.SaveCustomer(Actor, new CustomerEntity
            {
                CustomerId = Current == null?0:Current.CustomerId, CustomerCode = code.Text, FullName = name.Text, Phone = phone.Text, Email = email.Text, IdentityNumber = identity.Text, MemberTier = tier.Text, Address = address.Text, Notes = notes.Text, 
                Gender = gender.Text, DateOfBirth = dob.Checked?(DateTime?)dob.Value.Date:null, 
                PhotoData = selectedPhoto
            });
        }
    }
}
