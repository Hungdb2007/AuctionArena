using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AuctionArena.Services;
namespace AuctionArena.WinForms
{
    public sealed class ItemImagesForm : Form
    {
        readonly int actor, itemId;
        readonly PermissionFlags rights;
        readonly ItemImageService svc = new ItemImageService();
        readonly DataGridView grid;
        readonly PictureBox preview;
        public ItemImagesForm(int actor, int itemId, PermissionFlags rights)
        {
            this.actor = actor;
            this.itemId = itemId;
            this.rights = rights;
            Text = "Ảnh hiện vật #"+itemId;
            Width = 920;
            Height = 580;
            StartPosition = FormStartPosition.CenterParent;
            Theme.Apply(this);
            grid = Theme.Grid(20, 30, 430, 410);
            Controls.Add(grid);
            preview = new PictureBox
            {
                Left = 480, Top = 30, Width = 400, Height = 410, BackColor = Theme.Panel, 
                SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(preview);
            var add = Theme.Button("Thêm ảnh PNG/JPG", 20, 462, 190);
            add.Enabled = rights.Edit;
            Controls.Add(add);
            var remove = Theme.Button("Xóa ảnh chọn", 230, 462, 190);
            remove.Enabled = rights.Edit;
            Controls.Add(remove);
            var reload = Theme.Button("Tải lại", 480, 462, 145);
            Controls.Add(reload);
            var primary = Theme.Button("Đặt ảnh chính", 650, 462, 215);
            primary.Enabled = rights.Edit;
            Controls.Add(primary);
            primary.Click+=(s, e) => Theme.Safe(() => 
            {
                if (grid.CurrentRow == null)
                {
                    throw new BusinessException("Chọn một ảnh trước.");
                }
                svc.SetPrimary(actor, Convert.ToInt32(grid.CurrentRow.Cells["ItemImageId"].Value)); LoadData();
            });
            preview.DoubleClick+=(s, e) => Theme.Safe(() => 
            {
                if (preview.Image == null)
                {
                    return;
                }
                using (var dialog = new Form
                {
                    Text = "Xem ảnh hiện vật (phóng to)", Width = 1050, Height = 780, 
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    var picture = new PictureBox
                    {
                        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, 
                        Image = new Bitmap(preview.Image), BackColor = Color.Black
                    };
                    dialog.Controls.Add(picture); dialog.ShowDialog(this); picture.Image.Dispose();
                }
            });
            grid.SelectionChanged+=(s, e) => Theme.Safe(PreviewSelected);
            Shown+=(s, e) => LoadData();
            reload.Click+=(s, e) => LoadData();
            add.Click+=(s, e) => Theme.Safe(() => 
            {
                using (var picker = new OpenFileDialog
                {
                    Filter = "Ảnh PNG/JPEG|*.png;*.jpg;*.jpeg", Title = "Chọn ảnh hiện vật"
                })
                {
                    if (picker.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                    svc.Add(actor, itemId, picker.FileName); LoadData();
                }
            });
            remove.Click+=(s, e) => Theme.Safe(() => 
            {
                if (grid.CurrentRow == null)
                {
                    throw new BusinessException("Hãy chọn ảnh muốn xóa.");
                }
                int id = Convert.ToInt32(grid.CurrentRow.Cells["ItemImageId"].Value);
                if (MessageBox.Show("Xóa ảnh đã chọn?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
                // Giải phóng ảnh đang xem trước khi xóa file.
                var old = preview.Image; preview.Image = null; if (old != null)old.Dispose();
                svc.Remove(actor, id); LoadData();
            });
            FormClosed+=(s, e) => 
            {
                var old = preview.Image;
                preview.Image = null;
                if (old != null)old.Dispose();
            };
        }
        void LoadData()
        {
            Theme.Safe(() => 
            {
                grid.DataSource = svc.Get(actor, itemId); PreviewSelected();
            });
        }
        void PreviewSelected()
        {
            var old = preview.Image;
            preview.Image = null;
            if (old != null)old.Dispose();
            if (grid.CurrentRow == null)
            {
                return;
            }
            var value = Convert.ToString(grid.CurrentRow.Cells["RelativePath"].Value);
            string path = ItemImageService.FilePath(value);
            if (!File.Exists(path))
            {
                return;
            }
            // Dùng bản sao trong bộ nhớ để tránh khóa file khi người dùng xóa ảnh.
            using (var src = Image.FromFile(path))preview.Image = new Bitmap(src);
        }
    }
}
