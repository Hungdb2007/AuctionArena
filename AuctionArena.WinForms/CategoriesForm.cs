using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class CategoriesForm : CatalogFormBase<CategorieEntity>
    {
        readonly CatalogService service = new CatalogService();
        readonly TextBox name, code, description, sortOrder;
        readonly CheckBox active;
        public CategoriesForm(int actor, PermissionFlags flags):base("Loại hiện vật", actor, flags)
        {
            name = Field("Tên loại hiện vật", 18, 416, 420);
            code = Field("Mã loại", 470, 416, 180);
            description = Field("Mô tả loại hiện vật", 18, 510, 610);
            sortOrder = Field("Thứ tự hiển thị", 18, 598, 170);
            active = new CheckBox
            {
                Text = "Đang sử dụng", Left = 215, Top = 628, Width = 220, Checked = true, ForeColor = Theme.Text
            };
            Controls.Add(active);
            RegisterEditor(active, "Trạng thái");
            var count = Theme.Button("Hiện vật thuộc loại", 480, 598, 210);
            Controls.Add(count);
            count.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn loại hiện vật.");
                }
                int total = service.CategoryItemCount(Actor, Current.CategoryId);
                if (MessageBox.Show("Loại này có "+total+" hiện vật. Mở danh sách đã lọc?", 
                "Hiện vật thuộc loại", MessageBoxButtons.YesNo) == DialogResult.Yes)
                using (var form = new ItemsForm(Actor, new PermissionFlags
                {
                    View = true
                }, Current.CategoryId))form.ShowDialog(this);
            });
            MoveActions(710);
            Height = 830;
        }
        protected override object Fetch(string q)
        {
            return service.Categories(Actor, q);
        }
        protected override void Fill(CategorieEntity x)
        {
            name.Text = x.CategoryName;
            code.Text = x.CategoryCode;
            description.Text = x.Description;
            sortOrder.Text = x.SortOrder.ToString();
            active.Checked = x.IsActive;
        }
        protected override void Clear()
        {
            name.Clear();
            code.Clear();
            description.Clear();
            sortOrder.Text = "0";
            active.Checked = true;
        }
        protected override void Delete()
        {
            service.DeleteCategory(Actor, Current.CategoryId);
        }
        protected override void Save()
        {
            service.SaveCategory(Actor, new CategorieEntity
            {
                CategoryId = Current == null?0:Current.CategoryId, CategoryName = name.Text, CategoryCode = code.Text, Description = description.Text, 
                SortOrder = int.TryParse(sortOrder.Text, out var number)?number:-1, IsActive = active.Checked
            });
        }
    }
}
