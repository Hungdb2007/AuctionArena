using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class RolesForm : Form
    {
        readonly int actor;
        readonly PermissionFlags grant;
        readonly AdministrationService svc = new AdministrationService();
        readonly ComboBox roles;
        readonly DataGridView grid;
        readonly TextBox code, name, description;
        readonly CheckBox active;
        readonly Button saveGrants;
        BindingList<RolePermissionRow> data;
        int lastRole;
        bool loading, dirty;
        string detailsBefore = "";
        public RolesForm(int actor, PermissionFlags grant)
        {
            this.actor = actor;
            this.grant = grant;
            Text = "Nhóm quyền và ma trận RBAC";
            Width = 1110;
            Height = 870;
            MinimumSize = new System.Drawing.Size(1100, 810);
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            KeyPreview = true;
            Controls.Add(Theme.Label("Chọn nhóm quyền", 20, 15, 265));
            roles = new ComboBox
            {
                Left = 20, Top = 43, Width = 265, DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(roles);
            Controls.Add(Theme.Label("Mã nhóm", 305, 15, 160));
            code = Theme.Input(305, 43, 155);
            Controls.Add(code);
            Controls.Add(Theme.Label("Tên nhóm", 480, 15, 255));
            name = Theme.Input(480, 43, 235);
            Controls.Add(name);
            active = new CheckBox
            {
                Text = "Hoạt động", Left = 745, Top = 45, Width = 150
            };
            Controls.Add(active);
            Controls.Add(Theme.Label("Mô tả nhóm quyền", 20, 90, 290));
            description = Theme.Input(20, 117, 730);
            description.MaxLength = 300;
            Controls.Add(description);
            var create = Theme.Button("Nhóm mới", 20, 165, 145);
            create.Enabled = grant.Create;
            Controls.Add(create);
            var save = Theme.Button("Lưu nhóm", 178, 165, 145);
            save.Enabled = grant.Edit || grant.Create;
            Controls.Add(save);
            var clone = Theme.Button("Nhân bản", 337, 165, 145);
            clone.Enabled = grant.Create;
            Controls.Add(clone);
            var remove = Theme.Button("Xóa nhóm trống", 496, 165, 185);
            remove.Enabled = grant.Delete;
            Controls.Add(remove);
            var members = Theme.Button("Xem thành viên", 699, 165, 175);
            Controls.Add(members);
            grid = Theme.Grid(20, 235, 1045, 360);
            grid.ReadOnly = !grant.Edit;
            Controls.Add(grid);
            var allOn = Theme.Button("Chọn tất cả", 20, 615, 155);
            allOn.Enabled = grant.Edit;
            Controls.Add(allOn);
            var allOff = Theme.Button("Bỏ tất cả", 188, 615, 145);
            allOff.Enabled = grant.Edit;
            Controls.Add(allOff);
            var rowToggle = Theme.Button("Bật/tắt dòng", 347, 615, 155);
            rowToggle.Enabled = grant.Edit;
            Controls.Add(rowToggle);
            var column = new ComboBox
            {
                Left = 515, Top = 623, Width = 135, DropDownStyle = ComboBoxStyle.DropDownList
            };
            column.Items.AddRange(new object[]
            {
                "Xem", "Thêm", "Sửa", "Xóa", "In"
            });
            column.SelectedIndex = 0;
            Controls.Add(column);
            var colOn = Theme.Button("Bật cột", 668, 615, 125);
            colOn.Enabled = grant.Edit;
            Controls.Add(colOn);
            var colOff = Theme.Button("Tắt cột", 806, 615, 125);
            colOff.Enabled = grant.Edit;
            Controls.Add(colOff);
            saveGrants = Theme.Button("Lưu ma trận quyền", 20, 665, 230);
            saveGrants.Enabled = grant.Edit;
            Controls.Add(saveGrants);
            var revert = Theme.Button("Hoàn tác", 268, 665, 150);
            Controls.Add(revert);
            var notice = Theme.Label("Bật Thêm/Sửa/Xóa/In tự bật Xem. Nhóm Admin không được sửa hoặc xóa.", 20, 723, 945);
            Controls.Add(notice);
            create.Click+=(s, e) => 
            {
                if (!Discard())
                {
                    return;
                }
                loading = true;
                roles.SelectedIndex = -1;
                code.Clear();
                name.Clear();
                description.Clear();
                active.Checked = true;
                grid.DataSource = null;
                data = null;
                lastRole = 0;
                dirty = false;
                detailsBefore = Snapshot();
                loading = false;
            };
            save.Click+=(s, e) => Theme.Safe(() => 
            {
                var target = roles.SelectedItem as RoleOption;
                if (dirty && !Discard())
                {
                    return;
                }
                int id = target == null?0:target.Id;
                int result = svc.SaveRole(actor, id, code.Text, name.Text, active.Checked, description.Text);
                dirty = false; LoadRoles(result); MessageBox.Show("Đã lưu nhóm quyền.");
            });
            clone.Click+=(s, e) => Theme.Safe(() => 
            {
                if (!Discard())
                {
                    return;
                }
                var target = roles.SelectedItem as RoleOption; if (target == null)throw new BusinessException("Chọn nhóm để nhân bản.");
                using (var dlg = new Form
                {
                    Text = "Nhân bản nhóm", Width = 460, Height = 250, StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dlg); dlg.Controls.Add(Theme.Label("Mã nhóm mới", 20, 20, 400)); var c = Theme.Input(20, 48, 390); dlg.Controls.Add(c);
                    dlg.Controls.Add(Theme.Label("Tên nhóm mới", 20, 86, 400)); var n = Theme.Input(20, 114, 390); dlg.Controls.Add(n);
                    var yes = Theme.Button("Nhân bản", 40, 162, 160); yes.DialogResult = DialogResult.OK; dlg.Controls.Add(yes);
                    var no = Theme.Button("Hủy", 230, 162, 160); no.DialogResult = DialogResult.Cancel; dlg.Controls.Add(no);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        int id = svc.CloneRole(actor, target.Id, c.Text, n.Text); LoadRoles(id);
                    }
                }
            });
            remove.Click+=(s, e) => Theme.Safe(() => 
            {
                var target = roles.SelectedItem as RoleOption; if (target == null || !Discard())return;
                if (MessageBox.Show("Xóa nhóm "+target.Name+" nếu không có thành viên?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                svc.DeleteRole(actor, target.Id); dirty = false; LoadRoles(0);
            });
            members.Click+=(s, e) => Theme.Safe(() => 
            {
                var target = roles.SelectedItem as RoleOption; if (target == null)throw new BusinessException("Chọn nhóm quyền.");
                using (var dlg = new Form
                {
                    Text = "Thành viên: "+target.Name, Width = 790, Height = 500, StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dlg); var list = Theme.Grid(15, 15, 730, 400); list.DataSource = svc.GetRoleMembers(actor, target.Id); dlg.Controls.Add(list); dlg.ShowDialog(this);
                }
            });
            roles.SelectedIndexChanged+=(s, e) => 
            {
                if (loading)
                {
                    return;
                }
                if (!Discard())
                {
                    loading = true;
                    roles.SelectedItem = roles.Items.Cast<RoleOption>().FirstOrDefault(x => x.Id == lastRole);
                    loading = false;
                    return;
                }
                Theme.Safe(LoadRights);
            };
            saveGrants.Click+=(s, e) => Theme.Safe(() => 
            {
                var target = roles.SelectedItem as RoleOption;
                if (target == null || data == null)
                {
                    throw new BusinessException("Chọn nhóm cần lưu quyền.");
                }
                grid.EndEdit();
                if (MessageBox.Show("Áp dụng ma trận quyền cho các lần đăng nhập sau?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                svc.SaveRolePermissions(actor, target.Id, data.ToList()); LoadRights(); MessageBox.Show("Đã lưu quyền.");
            });
            revert.Click+=(s, e) => 
            {
                if (Discard())Theme.Safe(LoadRights);
            };
            allOn.Click+=(s, e) => SetAll(true);
            allOff.Click+=(s, e) => SetAll(false);
            rowToggle.Click+=(s, e) => 
            {
                if (!CanEditMatrix() || grid.CurrentRow == null)
                {
                    return;
                }
                var x = grid.CurrentRow.DataBoundItem as RolePermissionRow;
                if (x == null)
                {
                    return;
                }
                bool on = !(x.CanView && x.CanCreate && x.CanEdit && x.CanDelete && x.CanPrint);
                x.CanView = x.CanCreate = x.CanEdit = x.CanDelete = x.CanPrint = on;
                MarkDirty();
            };
            colOn.Click+=(s, e) => SetColumn(column.SelectedIndex, true);
            colOff.Click+=(s, e) => SetColumn(column.SelectedIndex, false);
            grid.CurrentCellDirtyStateChanged+=(s, e) => 
            {
                if (grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged+=(s, e) => 
            {
                if (loading || data == null || e.RowIndex<0 || !CanEditMatrix())
                {
                    return;
                }
                var x = grid.Rows[e.RowIndex].DataBoundItem as RolePermissionRow;
                if (x == null)
                {
                    return;
                }
                var changed = grid.Columns[e.ColumnIndex].DataPropertyName;
                if (changed == "CanView" && !x.CanView)
                {
                    x.CanCreate = x.CanEdit = x.CanDelete = x.CanPrint = false;
                }
                else if (x.CanCreate || x.CanEdit || x.CanDelete || x.CanPrint)x.CanView = true;
                MarkDirty();
            };
            FormClosing+=(s, e) => 
            {
                if (!Discard())e.Cancel = true;
            };
            KeyDown+=(s, e) => 
            {
                if (e.Control && e.KeyCode == Keys.S)
                {
                    saveGrants.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.Control && e.KeyCode == Keys.N && grant.Create)
                {
                    create.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.KeyCode == Keys.F5 && Discard())
                {
                    LoadRoles(lastRole);
                    e.SuppressKeyPress = true;
                }
                if (e.KeyCode == Keys.Delete && !grid.Focused && remove.Enabled)
                {
                    remove.PerformClick();
                    e.SuppressKeyPress = true;
                }
                if (e.KeyCode == Keys.Escape && Discard())
                {
                    LoadRights();
                    e.SuppressKeyPress = true;
                }
            };
            Shown+=(s, e) => Theme.Safe(() => LoadRoles(0));
        }
        string Snapshot()
        {
            return code.Text+"|"+name.Text+"|"+description.Text+"|"+active.Checked;
        }
        bool Discard()
        {
            return (!dirty && Snapshot() == detailsBefore) || MessageBox.Show("Bỏ thay đổi nhóm/quyền chưa lưu?", "Xác nhận", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }
        bool CanEditMatrix()
        {
            var r = roles.SelectedItem as RoleOption;
            return grant.Edit && r != null && r.Code != "ADMIN" && data != null;
        }
        void MarkDirty()
        {
            dirty = true;
            loading = true;
            try
            {
                data.ResetBindings();
            }
            finally
            {
                loading = false;
            }
        }
        void SetAll(bool on)
        {
            if (!CanEditMatrix())
            {
                return;
            }
            foreach (var x in data)x.CanView = x.CanCreate = x.CanEdit = x.CanDelete = x.CanPrint = on;
            MarkDirty();
        }
        void SetColumn(int column, bool on)
        {
            if (!CanEditMatrix())
            {
                return;
            }
            foreach (var x in data)
            {
                switch (column)
                {
                    case 0:x.CanView = on;
                    if (!on)x.CanCreate = x.CanEdit = x.CanDelete = x.CanPrint = false;
                    break;
                    case 1:x.CanCreate = on;
                    break;
                    case 2:x.CanEdit = on;
                    break;
                    case 3:x.CanDelete = on;
                    break;
                    case 4:x.CanPrint = on;
                    break;
                }
                if (x.CanCreate || x.CanEdit || x.CanDelete || x.CanPrint)x.CanView = true;
            }
            MarkDirty();
        }
        void LoadRoles(int id)
        {
            loading = true;
            try
            {
                roles.Items.Clear();
                foreach (var r in svc.GetRoles(actor))roles.Items.Add(r);
                roles.SelectedItem = roles.Items.Cast<RoleOption>().FirstOrDefault(x => x.Id == id)??roles.Items.Cast<RoleOption>().FirstOrDefault();
            }
            finally
            {
                loading = false;
            }
            LoadRights();
        }
        void LoadRights()
        {
            loading = true;
            try
            {
                var r = roles.SelectedItem as RoleOption;
                if (r == null)
                {
                    data = null;
                    grid.DataSource = null;
                    lastRole = 0;
                    dirty = false;
                    detailsBefore = Snapshot();
                    return;
                }
                code.Text = r.Code;
                name.Text = r.Name;
                description.Text = r.Description??"";
                active.Checked = r.Active;
                data = new BindingList<RolePermissionRow>(svc.GetRolePermissions(actor, r.Id));
                grid.DataSource = data;
                if (grid.Columns.Contains("PermissionId"))grid.Columns["PermissionId"].Visible = false;
                if (grid.Columns.Contains("Name"))grid.Columns["Name"].ReadOnly = true;
                grid.ReadOnly = !grant.Edit || r.Code == "ADMIN";
                lastRole = r.Id;
                dirty = false;
                detailsBefore = Snapshot();
            }
            finally
            {
                loading = false;
            }
        }
    }
}
