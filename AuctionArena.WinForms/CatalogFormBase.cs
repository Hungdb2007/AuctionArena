using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    // Form con định nghĩa cách tải dữ liệu, hiển thị lên ô nhập và lưu.
    public abstract class CatalogFormBase<T> : Form where T:class
    {
        protected readonly int Actor;
        protected readonly PermissionFlags Flags;
        protected readonly DataGridView Grid;
        protected readonly TextBox Search;
        protected readonly Button SaveButton, DeleteButton, ClearButton;
        protected T Current;
        readonly Button editButton, cancelButton, exportButton;
        readonly ErrorProvider errors = new ErrorProvider();
        readonly List<Control> editors = new List<Control>();
        readonly Dictionary<Control, string> names = new Dictionary<Control, string>();
        readonly HashSet<Control> immutable = new HashSet<Control>();
        bool loading, editing, creating, suppress;
        string initial = "";
        protected CatalogFormBase(string title, int actor, PermissionFlags flags)
        {
            Actor = actor;
            Flags = flags;
            Text = title;
            Width = 1060;
            Height = 790;
            MinimumSize = new Size(940, 680);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            KeyPreview = true;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            Search = Theme.Input(18, 18, 330);
            Controls.Add(Search);
            var refresh = Theme.Button("Tìm / tải lại", 365, 14, 145);
            Controls.Add(refresh);
            refresh.Click+=(s, e) => Reload();
            Search.KeyDown+=(s, e) => 
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Reload();
                }
            };
            exportButton = Theme.Button("Xuất Excel", 525, 14, 155);
            Controls.Add(exportButton);
            exportButton.Click+=(s, e) => Theme.Safe(() => ExcelExporter.Export(Grid));
            Grid = Theme.Grid(18, 60, 1000, 330);
            Grid.Anchor = AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            Controls.Add(Grid);
            Grid.SelectionChanged+=(s, e) => 
            {
                if (loading || suppress)
                {
                    return;
                }
                if (editing && HasUnsavedChanges() && !ConfirmDiscard())
                {
                    return;
                }
                editing = false;
                creating = false;
                var obj = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as T;
                Current = obj;
                if (obj != null)Fill(obj);
                else Clear();
                RememberCurrentValues();
                ApplyMode();
            };
            SaveButton = Theme.Button("Lưu thông tin", 18, 620, 170);
            Controls.Add(SaveButton);
            ClearButton = Theme.Button("Thêm mới", 200, 620, 145);
            Controls.Add(ClearButton);
            DeleteButton = Theme.Button("Xóa / ngừng", 355, 620, 180);
            Controls.Add(DeleteButton);
            editButton = Theme.Button("Sửa", 545, 620, 115);
            Controls.Add(editButton);
            cancelButton = Theme.Button("Hủy sửa", 670, 620, 130);
            Controls.Add(cancelButton);
            var count = Theme.Label("Chưa có dữ liệu", 818, 28, 205);
            count.Name = "RowCount";
            Controls.Add(count);
            ClearButton.Click+=(s, e) => 
            {
                if (!Flags.Create || !ConfirmDiscard())
                {
                    return;
                }
                suppress = true;
                try
                {
                    Grid.ClearSelection();
                    Grid.CurrentCell = null;
                    Current = null;
                    Clear();
                }
                finally
                {
                    suppress = false;
                }
                editing = true;
                creating = true;
                GenerateCodeIfBlank();
                RememberCurrentValues();
                ApplyMode();
            };
            editButton.Click+=(s, e) => 
            {
                if (Current == null || !Flags.Edit)
                {
                    return;
                }
                editing = true;
                creating = false;
                RememberCurrentValues();
                ApplyMode();
            };
            cancelButton.Click+=(s, e) => 
            {
                if (!ConfirmDiscard())
                {
                    return;
                }
                editing = false;
                creating = false;
                if (Current != null)Fill(Current);
                else Clear();
                RememberCurrentValues();
                ApplyMode();
            };
            SaveButton.Click+=(s, e) => 
            {
                if (!editing)
                {
                    return;
                }
                errors.Clear();
                try
                {
                    string key = CurrentCode();
                    Save();
                    editing = false;
                    creating = false;
                    ReloadCore(key);
                    MessageBox.Show("Đã lưu thành công.", "Auction Arena");
                }
                catch (Exception ex)
                {
                    var field = FindInvalidField(ex.Message);
                    if (field != null)
                    {
                        errors.SetError(field, ex.Message);
                        field.Focus();
                    }
                    MessageBox.Show(ex.Message, "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            DeleteButton.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null || !Flags.Delete)
                {
                    throw new BusinessException("Chọn bản ghi có quyền xóa.");
                }
                if (MessageBox.Show("Nếu đã có chứng từ, hồ sơ sẽ chuyển Ngừng sử dụng (không xóa lịch sử). Tiếp tục?", 
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)return;
                Delete(); Current = null; editing = false; creating = false; ReloadCore(null);
            });
            KeyDown+=(s, e) => 
            {
                if (e.Control && e.KeyCode == Keys.N)
                {
                    ClearButton.PerformClick();
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.S)
                {
                    SaveButton.PerformClick();
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.F)
                {
                    Search.Focus();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.F5)
                {
                    Reload();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    cancelButton.PerformClick();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Delete && !editing && Search.Focused == false && Grid.Focused)
                {
                    DeleteButton.PerformClick();
                    e.SuppressKeyPress = true;
                }
            };
            FormClosing+=(s, e) => 
            {
                if (!ConfirmDiscard())e.Cancel = true;
            };
            Shown+=(s, e) => ReloadCore(null);
            ApplyMode();
        }
        string CurrentCode()
        {
            var code = names.FirstOrDefault(x => x.Value.StartsWith("Mã ", StringComparison.OrdinalIgnoreCase));
            return code.Key == null?null:code.Key.Text.Trim();
        }
        void GenerateCodeIfBlank()
        {
            var code = names.FirstOrDefault(x => x.Value.StartsWith("Mã ", StringComparison.OrdinalIgnoreCase));
            if (code.Key != null && string.IsNullOrWhiteSpace(code.Key.Text))
            {
                var prefix = typeof(T).Name.StartsWith("Customer")?"KH":typeof(T).Name.StartsWith("Consignor")?"CH":
                typeof(T).Name.StartsWith("Item")?"HV":"LOAI";
                code.Key.Text = prefix+Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            }
        }
        string EditorValue(Control control)
        {
            var cb = control as CheckBox;
            if (cb != null)
            {
                return cb.Checked?"1":"0";
            }
            var date = control as DateTimePicker;
            if (date != null)
            {
                return date.Checked+"|"+date.Value.ToString("O");
            }
            var combo = control as ComboBox;
            if (combo != null)
            {
                return combo.SelectedIndex.ToString();
            }
            return control.Text??"";
        }
        string Snapshot()
        {
            return string.Join("\u001f", editors.Select(EditorValue));
        }
        void RememberCurrentValues()
        {
            initial = Snapshot();
        }
        bool HasUnsavedChanges()
        {
            return editing && initial != Snapshot();
        }
        bool ConfirmDiscard()
        {
            return !HasUnsavedChanges() || 
            MessageBox.Show("Có thông tin chưa lưu. Bỏ thay đổi?", "Xác nhận", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }
        Control FindInvalidField(string message)
        {
            var text = (message??"").ToLowerInvariant();
            foreach (var kv in names)if (text.Contains(kv.Value.ToLowerInvariant()))return kv.Key;
            string[] terms = 
            {
                "email", "điện thoại", "cccd", "mã", "giá", "tên", "ngày"
            };
            foreach (var t in terms)if (text.Contains(t))
            {
                var match = names.FirstOrDefault(kv => kv.Value.ToLowerInvariant().Contains(t));
                if (match.Key != null)
                {
                    return match.Key;
                }
            }
            return editors.FirstOrDefault(x => x.Focused)??editors.FirstOrDefault();
        }
        void ApplyMode()
        {
            foreach (var editor in editors)
            {
                bool enabled = editing && !immutable.Contains(editor);
                if (editor is TextBox)((TextBox)editor).ReadOnly = !enabled;
                else editor.Enabled = enabled;
            }
            SaveButton.Enabled = editing && (creating?Flags.Create:Flags.Edit);
            ClearButton.Enabled = Flags.Create;
            DeleteButton.Enabled = !editing && Current != null && Flags.Delete;
            editButton.Enabled = !editing && Current != null && Flags.Edit;
            cancelButton.Enabled = editing;
            OnModeChanged(editing);
        }
        // Form con cập nhật các thao tác nghiệp vụ không lưu bằng nút Lưu thông tin.
        protected virtual void OnModeChanged(bool isEditing)
        {
        }
        protected void RegisterEditor(Control c, string name = "")
        {
            editors.Add(c);
            names[c] = name;
        }
        protected void ReadOnlyAlways(Control c)
        {
            immutable.Add(c);
            if (c is TextBox)((TextBox)c).ReadOnly = true;
        }
        protected void MoveActions(int top)
        {
            SaveButton.Top = top;
            ClearButton.Top = top;
            DeleteButton.Top = top;
            editButton.Top = top;
            cancelButton.Top = top;
        }
        protected TextBox Field(string name, int x, int y, int width = 190)
        {
            Controls.Add(Theme.Label(name, x, y, width));
            var input = Theme.Input(x, y+27, width);
            Controls.Add(input);
            RegisterEditor(input, name);
            input.ReadOnly = true;
            return input;
        }
        protected ComboBox Combo(string name, int x, int y, int width = 190)
        {
            Controls.Add(Theme.Label(name, x, y, width));
            var combo = new ComboBox
            {
                Left = x, Top = y+27, Width = width, DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(combo);
            RegisterEditor(combo, name);
            combo.Enabled = false;
            return combo;
        }
        protected void Reload()
        {
            if (!ConfirmDiscard())
            {
                return;
            }
            ReloadCore(CurrentCode());
        }
        protected void ReloadCommitted()
        {
            editing = false;
            creating = false;
            ReloadCore(CurrentCode());
        }
        void ReloadCore(string preserve)
        {
            Theme.Safe(() => 
            {
                loading = true; errors.Clear();
                try
                {
                    Grid.DataSource = Fetch(Search.Text.Trim());
                    var codeColumn = Grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(x => x.Name.EndsWith("Code"));
                    if (codeColumn != null && !string.IsNullOrWhiteSpace(preserve))
                    foreach (DataGridViewRow row in Grid.Rows)
                    if (string.Equals(Convert.ToString(row.Cells[codeColumn.Name].Value), preserve, 
                    StringComparison.OrdinalIgnoreCase))
                    {
                        Grid.CurrentCell = row.Cells[codeColumn.Index]; break;
                    }
                    Current = Grid.CurrentRow == null?null:Grid.CurrentRow.DataBoundItem as T;
                    if (Current != null)Fill(Current); else Clear();
                    var count = Controls.Find("RowCount", false).FirstOrDefault();
                    if (count != null)count.Text = Grid.Rows.Count == 0?"Chưa có dữ liệu":"Tổng: "+Grid.Rows.Count+" bản ghi";
                    editing = false; creating = false; RememberCurrentValues();
                }
                finally
                {
                    loading = false; ApplyMode();
                }
            });
        }
        protected abstract object Fetch(string query);
        protected abstract void Fill(T item);
        protected abstract void Clear();
        protected abstract void Save();
        protected abstract void Delete();
        protected static int Id(ComboBox box)
        {
            return box.SelectedValue is int?(int)box.SelectedValue:0;
        }
        protected static decimal Money(TextBox box)
        {
            decimal d;
            if (!decimal.TryParse(box.Text, out d) || d<0)
            throw new BusinessException("Số tiền không hợp lệ: "+box.Text);
            return d;
        }
    }
}
