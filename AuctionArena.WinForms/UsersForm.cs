using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    // Quản lý tài khoản: Thêm, Sửa, Xóa/Khóa và xem nhật ký.
    // Khi nhấn Lưu, Form gọi AdministrationService; không tự chạy câu SQL.
    public sealed class UsersForm : Form
    {
        readonly int actor;
        readonly PermissionFlags rights;
        readonly AdministrationService service = new AdministrationService();
        readonly DataGridView grid, logs;
        readonly TextBox username, fullname, email, phone, password, reason, search, action;
        readonly ComboBox role, roleFilter, stateFilter;
        readonly CheckBox active;
        readonly DateTimePicker from, to;
        readonly ErrorProvider errors;
        readonly Button save, edit, delete, cancel;
        int selected, previousRowIndex = -1;
        bool loading, editing;
        string before = "";
        public UsersForm(int actor, PermissionFlags rights)
        {
            this.actor = actor;
            this.rights = rights;
            Text = "Tài khoản & Nhật ký";
            Width = 1100;
            Height = 835;
            MinimumSize = new Size(1060, 730);
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            KeyPreview = true;
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(tabs);
            var users = new TabPage("Người dùng")
            {
                BackColor = Theme.Bg
            };
            tabs.TabPages.Add(users);
            var history = new TabPage("Nhật ký hoạt động")
            {
                BackColor = Theme.Bg
            };
            tabs.TabPages.Add(history);
            users.Controls.Add(Theme.Label("Tìm tên đăng nhập / họ tên / email", 20, 14, 340));
            search = Theme.Input(20, 44, 315);
            users.Controls.Add(search);
            roleFilter = new ComboBox
            {
                Left = 350, Top = 44, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList
            };
            users.Controls.Add(roleFilter);
            stateFilter = new ComboBox
            {
                Left = 600, Top = 44, Width = 165, DropDownStyle = ComboBoxStyle.DropDownList
            };
            stateFilter.Items.AddRange(new object[]
            {
                "Tất cả", "Hoạt động", "Khóa"
            });
            stateFilter.SelectedIndex = 0;
            users.Controls.Add(stateFilter);
            var find = Theme.Button("Tìm / tải lại", 780, 39, 160);
            users.Controls.Add(find);
            grid = Theme.Grid(20, 93, 1015, 255);
            grid.Anchor = AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            users.Controls.Add(grid);
            username = Make(users, "Tên đăng nhập", 20, 368, 220);
            fullname = Make(users, "Họ và tên", 265, 368, 295);
            email = Make(users, "Email", 585, 368, 230);
            phone = Make(users, "Số điện thoại", 840, 368, 180);
            role = new ComboBox
            {
                Left = 20, Top = 475, Width = 270, DropDownStyle = ComboBoxStyle.DropDownList
            };
            users.Controls.Add(role);
            users.Controls.Add(Theme.Label("Nhóm quyền", 20, 447, 220));
            active = new CheckBox
            {
                Left = 318, Top = 476, Text = "Hoạt động", Width = 145
            };
            users.Controls.Add(active);
            password = Make(users, "Mật khẩu ban đầu (chỉ khi thêm)", 475, 447, 270);
            password.UseSystemPasswordChar = true;
            reason = Make(users, "Lý do khóa", 770, 447, 250);
            var add = Theme.Button("Thêm mới", 20, 548, 155);
            add.Enabled = rights.Create;
            users.Controls.Add(add);
            edit = Theme.Button("Sửa", 190, 548, 115);
            users.Controls.Add(edit);
            save = Theme.Button("Lưu", 325, 548, 115);
            users.Controls.Add(save);
            cancel = Theme.Button("Hủy sửa", 460, 548, 125);
            users.Controls.Add(cancel);
            delete = Theme.Button("Xóa / khóa", 600, 548, 165);
            users.Controls.Add(delete);
            var reset = Theme.Button("Đặt lại mật khẩu", 780, 548, 230);
            reset.Enabled = rights.Edit;
            users.Controls.Add(reset);
            var export = Theme.Button("Xuất Excel", 20, 604, 175);
            users.Controls.Add(export);
            var message = Theme.Label("Tài khoản đã có giao dịch sẽ được khóa thay vì xóa. Mật khẩu tạm chỉ hiện một lần.", 210, 608, 800);
            users.Controls.Add(message);
            errors = new ErrorProvider(this)
            {
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
            FormClosed+=(s, e) => errors.Dispose();
            add.Click+=(s, e) => 
            {
                if (!Discard())
                {
                    return;
                }
                Clear();
                editing = true;
                RememberCurrentValues();
                Toggle();
                username.Focus();
            };
            edit.Click+=(s, e) => 
            {
                if (selected<=0)
                {
                    return;
                }
                editing = true;
                RememberCurrentValues();
                Toggle();
                fullname.Focus();
            };
            cancel.Click+=(s, e) => 
            {
                if (Discard())LoadData();
            };
            save.Click+=(s, e) => 
            {
                errors.Clear();
                if (!editing)
                {
                    return;
                }
                if (username.Text.Trim().Length<3)
                {
                    Fail(username, "Tên đăng nhập quá ngắn.");
                    return;
                }
                if (fullname.Text.Trim().Length<2)
                {
                    Fail(fullname, "Nhập họ và tên.");
                    return;
                }
                if (role.SelectedItem == null)
                {
                    Fail(role, "Chọn nhóm quyền.");
                    return;
                }
                if (selected == 0 && !AccountService.StrongPassword(password.Text))
                {
                    Fail(password, "Mật khẩu ≥8 ký tự, hoa, thường và số.");
                    return;
                }
                if (selected != 0 && active.Checked == false && string.IsNullOrWhiteSpace(reason.Text))
                {
                    Fail(reason, "Nhập lý do khóa.");
                    return;
                }
                Theme.Safe(() => 
                {
                    var r = (RoleOption)role.SelectedItem;
                    service.SaveUser(actor, selected, username.Text, fullname.Text, r.Id, active.Checked, 
                    selected == 0?password.Text:null, email.Text, phone.Text, reason.Text);
                    string savedUsername = username.Text.Trim();
                    editing = false; LoadData(savedUsername); MessageBox.Show("Đã lưu tài khoản.");
                });
            };
            delete.Enabled = rights.Delete;
            delete.Click+=(s, e) => Theme.Safe(() => 
            {
                if (selected<=0 || !Discard())
                {
                    return;
                }
                var why = Prompt("Lý do xóa / khóa tài khoản:"); if (why == null)return;
                if (MessageBox.Show("Xác nhận xóa/khóa tài khoản "+username.Text+"?", "Xác nhận", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
                bool removed = service.DeleteOrLockUser(actor, selected, why); LoadData();
                MessageBox.Show(removed?"Đã xóa tài khoản chưa sử dụng.":"Đã khóa tài khoản có lịch sử.");
            });
            reset.Click+=(s, e) => Theme.Safe(() => 
            {
                if (selected<=0 || !Discard())
                {
                    return;
                }
                if (MessageBox.Show("Đặt lại mật khẩu cho "+username.Text+"?", "Xác nhận", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)return;
                string temporary = service.ResetPassword(actor, selected);
                MessageBox.Show("Mật khẩu tạm (chỉ hiển thị lần này):\n"+temporary+"\nNgười dùng phải đổi mật khẩu khi đăng nhập.");
                LoadData();
            });
            find.Click+=(s, e) => 
            {
                if (Discard())Theme.Safe(LoadFiltered);
            };
            roleFilter.SelectedIndexChanged+=(s, e) => 
            {
                if (!loading && Discard())Theme.Safe(LoadFiltered);
            };
            stateFilter.SelectedIndexChanged+=(s, e) => 
            {
                if (!loading && Discard())Theme.Safe(LoadFiltered);
            };
            search.KeyDown+=(s, e) => 
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    find.PerformClick();
                }
            };
            export.Click+=(s, e) => Theme.Safe(() => ExcelExporter.Export(grid));
            grid.SelectionChanged+=(s, e) => 
            {
                if (loading || grid.CurrentRow == null)
                {
                    return;
                }
                if (editing && !Discard())
                {
                    loading = true;
                    try
                    {
                        if (previousRowIndex>=0 && previousRowIndex<grid.Rows.Count)
                        grid.CurrentCell = grid.Rows[previousRowIndex].Cells[0];
                    }
                    finally
                    {
                        loading = false;
                    }
                    return;
                }
                var u = grid.CurrentRow.DataBoundItem as UserRow;
                if (u == null)
                {
                    return;
                }
                editing = false;
                selected = u.UserId;
                username.Text = u.Username;
                fullname.Text = u.FullName;
                email.Text = u.Email??"";
                phone.Text = u.Phone??"";
                password.Clear();
                reason.Clear();
                active.Checked = u.IsActive;
                role.SelectedItem = role.Items.Cast<RoleOption>().FirstOrDefault(r => r.Id == u.RoleId);
                previousRowIndex = grid.CurrentRow.Index;
                RememberCurrentValues();
                Toggle();
            };
            grid.CellDoubleClick+=(s, e) => 
            {
                if (e.RowIndex>=0)edit.PerformClick();
            };
            history.Controls.Add(Theme.Label("Mã hành động", 20, 18, 240));
            action = Theme.Input(20, 47, 240);
            history.Controls.Add(action);
            history.Controls.Add(Theme.Label("Từ ngày", 280, 18, 175));
            from = new DateTimePicker
            {
                Left = 280, Top = 47, Width = 175, Value = DateTime.Today.AddMonths(-1)
            };
            history.Controls.Add(from);
            history.Controls.Add(Theme.Label("Đến ngày", 470, 18, 175));
            to = new DateTimePicker
            {
                Left = 470, Top = 47, Width = 175
            };
            history.Controls.Add(to);
            var loadLog = Theme.Button("Xem nhật ký", 670, 43, 165);
            history.Controls.Add(loadLog);
            var excelLog = Theme.Button("Xuất Excel", 850, 43, 165);
            history.Controls.Add(excelLog);
            logs = Theme.Grid(20, 105, 1000, 550);
            logs.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
            history.Controls.Add(logs);
            loadLog.Click+=(s, e) => Theme.Safe(() => logs.DataSource = service.UserAudit(actor, null, action.Text, from.Value, to.Value));
            excelLog.Click+=(s, e) => Theme.Safe(() => ExcelExporter.Export(logs));
            tabs.SelectedIndexChanged+=(s, e) => 
            {
                if (tabs.SelectedIndex == 1)loadLog.PerformClick();
            };
            KeyDown+=(s, e) => 
            {
                if (e.KeyCode == Keys.F5)
                {
                    if (tabs.SelectedIndex == 0)find.PerformClick();
                    else loadLog.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.Control && e.KeyCode == Keys.N && rights.Create)
                {
                    add.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.Control && e.KeyCode == Keys.S && save.Enabled)
                {
                    save.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.Control && e.KeyCode == Keys.F)
                {
                    search.Focus();
                    e.SuppressKeyPress = true;
                }
                if (e.KeyCode == Keys.Escape && editing)
                {
                    cancel.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.KeyCode == Keys.Delete && !editing && delete.Enabled)
                {
                    delete.PerformClick();
                    e.SuppressKeyPress = true;
                }
            };
            FormClosing+=(s, e) => 
            {
                if (!Discard())e.Cancel = true;
            };
            Shown+=(s, e) => Theme.Safe(LoadData);
            Toggle();
        }
        static TextBox Make(Control parent, string caption, int x, int y, int width)
        {
            parent.Controls.Add(Theme.Label(caption, x, y, width));
            var box = Theme.Input(x, y+28, width);
            parent.Controls.Add(box);
            return box;
        }
        static string Prompt(string title)
        {
            using (var f = new Form
            {
                Text = title, Width = 490, Height = 200, StartPosition = FormStartPosition.CenterParent
            })
            {
                Theme.Apply(f);
                f.Controls.Add(Theme.Label(title, 20, 15, 435));
                var input = Theme.Input(20, 51, 425);
                f.Controls.Add(input);
                var ok = Theme.Button("Xác nhận", 40, 98, 175);
                var no = Theme.Button("Hủy", 230, 98, 175);
                f.Controls.Add(ok);
                f.Controls.Add(no);
                ok.DialogResult = DialogResult.OK;
                no.DialogResult = DialogResult.Cancel;
                f.AcceptButton = ok;
                f.CancelButton = no;
                return f.ShowDialog() == DialogResult.OK && input.Text.Trim().Length>=5?input.Text.Trim():null;
            }
        }
        void Fail(Control target, string error)
        {
            errors.SetError(target, error);
            target.Focus();
        }
        string Snapshot()
        {
            return string.Join("|", new[]
            {
                username.Text, fullname.Text, email.Text, phone.Text, reason.Text, 
                active.Checked.ToString(), password.Text, (role.SelectedItem as RoleOption)?.Id.ToString()??""
            });
        }
        void RememberCurrentValues()
        {
            before = Snapshot();
        }
        bool Discard()
        {
            return !editing || Snapshot() == before || MessageBox.Show("Bỏ thay đổi chưa lưu?", "Xác nhận", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }
        void Toggle()
        {
            username.ReadOnly = !editing || selected>0;
            fullname.ReadOnly = !editing;
            email.ReadOnly = !editing;
            phone.ReadOnly = !editing;
            reason.ReadOnly = !editing;
            password.ReadOnly = !editing || selected>0;
            role.Enabled = editing;
            active.Enabled = editing;
            save.Enabled = editing && (selected == 0?rights.Create:rights.Edit);
            edit.Enabled = !editing && selected>0 && rights.Edit;
            cancel.Enabled = editing;
        }
        void Clear()
        {
            loading = true;
            try
            {
                selected = 0;
                username.Clear();
                fullname.Clear();
                email.Clear();
                phone.Clear();
                password.Clear();
                reason.Clear();
                active.Checked = true;
                role.SelectedIndex = -1;
                grid.ClearSelection();
            }
            finally
            {
                loading = false;
            }
            Toggle();
        }
        void LoadFiltered()
        {
            loading = true;
            try
            {
                var choice = roleFilter.SelectedItem as RoleOption;
                int? roleId = choice == null || choice.Id == 0?(int?)null:choice.Id;
                bool? state = stateFilter.SelectedIndex == 0?(bool?)null:stateFilter.SelectedIndex == 1;
                grid.DataSource = service.GetUsers(actor, search.Text.Trim(), roleId, state);
                if (grid.Columns.Contains("RoleId"))grid.Columns["RoleId"].Visible = false;
                editing = false;
                selected = 0;
                username.Clear();
                fullname.Clear();
                email.Clear();
                phone.Clear();
                password.Clear();
                reason.Clear();
                active.Checked = true;
                role.SelectedIndex = -1;
                grid.ClearSelection();
                RememberCurrentValues();
            }
            finally
            {
                loading = false;
                Toggle();
            }
        }
        void LoadData()
        {
            LoadData(null);
        }
        void LoadData(string restoreUser)
        {
            loading = true;
            try
            {
                role.Items.Clear();
                roleFilter.Items.Clear();
                roleFilter.Items.Add(new RoleOption
                {
                    Id = 0, Name = "Tất cả nhóm quyền"
                });
                foreach (var r in service.GetRoles(actor, true))
                {
                    role.Items.Add(r);
                    roleFilter.Items.Add(r);
                }
                roleFilter.SelectedIndex = 0;
                grid.DataSource = service.GetUsers(actor, search.Text.Trim());
                if (grid.Columns.Contains("RoleId"))grid.Columns["RoleId"].Visible = false;
                selected = 0;
                editing = false;
                username.Clear();
                fullname.Clear();
                email.Clear();
                phone.Clear();
                password.Clear();
                reason.Clear();
                active.Checked = true;
                role.SelectedIndex = -1;
                grid.ClearSelection();
                RememberCurrentValues();
                if (!string.IsNullOrWhiteSpace(restoreUser))
                {
                    foreach (DataGridViewRow line in grid.Rows)
                    {
                        var item = line.DataBoundItem as UserRow;
                        if (item != null && item.Username == restoreUser && line.Cells.Count>0)
                        {
                            grid.CurrentCell = line.Cells[0];
                            break;
                        }
                    }
                }
            }
            finally
            {
                loading = false;
                Toggle();
            }
            // Chọn lại bản ghi đã lưu để tiếp tục làm việc.
            if (!string.IsNullOrWhiteSpace(restoreUser) && grid.CurrentRow != null)
            {
                var chosen = grid.CurrentRow.DataBoundItem as UserRow;
                if (chosen != null && chosen.Username == restoreUser)
                {
                    selected = chosen.UserId;
                    username.Text = chosen.Username;
                    fullname.Text = chosen.FullName;
                    email.Text = chosen.Email??"";
                    phone.Text = chosen.Phone??"";
                    active.Checked = chosen.IsActive;
                    role.SelectedItem = role.Items.Cast<RoleOption>().FirstOrDefault(x => x.Id == chosen.RoleId);
                    RememberCurrentValues();
                    Toggle();
                }
            }
        }
    }
}
