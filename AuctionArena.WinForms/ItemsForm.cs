using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AuctionArena.Services;
using AuctionArena.Data.Models;
namespace AuctionArena.WinForms
{
    public sealed class ItemsForm : CatalogFormBase<ItemEntity>
    {
        readonly CatalogService service = new CatalogService();
        readonly TextBox code, name, price, highPrice, appraisal, description, origin, condition, location, era, dimensions, material, appraisalNotes;
        readonly ComboBox category, owner, statusFilter, categoryFilter, ownerFilter;
        readonly Button appraise, reject, needs, notesEntry;
        readonly Label appraisalHint;
        decimal? minFilter, maxFilter;
        int? presetCategory;
        public ItemsForm(int actor, PermissionFlags flags, int? selectedCategory = null):base("Hiện vật và thẩm định", actor, flags)
        {
            // Lọc danh sách hiện vật theo loại đã được truyền vào từ màn Loại hiện vật.
            presetCategory = selectedCategory;
            // Không hiển thị mã khóa ngoại và RowVersion trong danh sách thao tác.
            // Các thuộc tính vẫn tồn tại trên Entity, không ảnh hưởng lưu/đọc dữ liệu.
            Grid.DataBindingComplete+=(s, e) => 
            {
                foreach (var field in new[]
                {
                    "ItemId", "CategoryId", "ConsignorId", "CreatedAt", "RowVersion"
                })
                if (Grid.Columns.Contains(field))Grid.Columns[field].Visible = false;
                foreach (var pair in new []
                {
                    new
                    {
                        Name = "ItemCode", Title = "Mã hiện vật"
                    }, 
                    new
                    {
                        Name = "ItemName", Title = "Tên hiện vật"
                    }, 
                    new
                    {
                        Name = "Description", Title = "Mô tả"
                    }, 
                    new
                    {
                        Name = "EstimatePrice", Title = "Giá ước tính"
                    }, 
                    new
                    {
                        Name = "AppraisedPrice", Title = "Giá thẩm định"
                    }, 
                    new
                    {
                        Name = "ItemStatus", Title = "Trạng thái"
                    }
                })
                if (Grid.Columns.Contains(pair.Name))Grid.Columns[pair.Name].HeaderText = pair.Title;
                Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            };
            code = Field("Mã hiện vật", 18, 408);
            name = Field("Tên hiện vật", 230, 408, 350);
            price = Field("Giá ước tính", 602, 408);
            category = Combo("Loại hiện vật", 18, 492);
            owner = Combo("Chủ hàng", 230, 492, 350);
            appraisal = Field("Giá thẩm định", 602, 492);
            ReadOnlyAlways(appraisal);
            appraisalHint = Theme.Label("Chọn hiện vật đã lưu để thẩm định.", 810, 510, 205);
            appraisalHint.Height = 55;
            Controls.Add(appraisalHint);
            description = Field("Mô tả", 18, 570, 440);
            origin = Field("Nguồn gốc", 18, 660, 440);
            condition = Field("Tình trạng bảo quản", 18, 744, 500);
            location = Field("Vị trí trong kho (chỉ đọc)", 18, 827, 330);
            ReadOnlyAlways(location);
            era = Field("Niên đại", 18, 918, 205);
            dimensions = Field("Kích thước", 243, 918, 245);
            material = Field("Chất liệu", 510, 918, 290);
            highPrice = Field("Giá ước tính cao", 18, 1008, 205);
            appraisalNotes = Field("Ghi chú / lý do thẩm định", 245, 1008, 590);
            ReadOnlyAlways(appraisalNotes);
            notesEntry = Theme.Button("Nhập lý do", 850, 1032, 135);
            Controls.Add(notesEntry);
            notesEntry.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn hiện vật.");
                }
                using (var dlg = new Form
                {
                    Text = "Ghi chú thẩm định", Width = 600, Height = 245, 
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dlg);
                    var field = Theme.Input(16, 47, 550); field.MaxLength = 400; field.Text = appraisalNotes.Text;
                    dlg.Controls.Add(Theme.Label("Nhập ghi chú/lý do (tối đa 400 ký tự)", 16, 18, 540)); dlg.Controls.Add(field);
                    var ok = Theme.Button("Áp dụng", 220, 115, 170); dlg.Controls.Add(ok);
                    ok.Click+=(sender, args) => 
                    {
                        appraisalNotes.Text = field.Text; dlg.DialogResult = DialogResult.OK;
                    };
                    dlg.ShowDialog(this);
                }
            });
            statusFilter = new ComboBox
            {
                Left = 18, Top = 1140, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList
            };
            statusFilter.Items.AddRange(new object[]
            {
                "Mọi trạng thái", "PendingAppraisal", "NeedInfo", "Approved", "Rejected", 
                "InStock", "InAuction", "Sold", "Delivered", "Returned"
            });
            statusFilter.SelectedIndex = 0;
            Controls.Add(statusFilter);
            categoryFilter = new ComboBox
            {
                Left = 224, Top = 1140, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(categoryFilter);
            ownerFilter = new ComboBox
            {
                Left = 490, Top = 1140, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(ownerFilter);
            var advanced = Theme.Button("Lọc", 745, 1135, 105);
            Controls.Add(advanced);
            advanced.Click+=(s, e) => Reload();
            var clearFilters = Theme.Button("Xóa lọc", 858, 1135, 125);
            Controls.Add(clearFilters);
            clearFilters.Click+=(s, e) => 
            {
                statusFilter.SelectedIndex = 0;
                categoryFilter.SelectedIndex = 0;
                ownerFilter.SelectedIndex = 0;
                minFilter = null;
                maxFilter = null;
                Reload();
            };
            var prices = Theme.Button("Lọc khoảng giá", 18, 1190, 185);
            Controls.Add(prices);
            prices.Click+=(s, e) => Theme.Safe(() => 
            {
                using (var dlg = new Form
                {
                    Text = "Khoảng giá ước tính thấp", Width = 480, Height = 235, 
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dlg);
                    var lo = Theme.Input(18, 55, 180); var hi = Theme.Input(238, 55, 180);
                    lo.Text = minFilter.HasValue?minFilter.Value.ToString("0.##"):"";
                    hi.Text = maxFilter.HasValue?maxFilter.Value.ToString("0.##"):"";
                    dlg.Controls.Add(Theme.Label("Giá từ", 18, 22, 200)); dlg.Controls.Add(Theme.Label("Giá đến", 238, 22, 200));
                    dlg.Controls.Add(lo); dlg.Controls.Add(hi);
                    var apply = Theme.Button("Áp dụng", 125, 115, 180); dlg.Controls.Add(apply);
                    apply.Click+=(sender, args) => Theme.Safe(() => 
                    {
                        decimal a, b; if (lo.Text.Trim().Length>0 && (!decimal.TryParse(lo.Text, out a) || a<0))
                        throw new BusinessException("Giá tối thiểu không hợp lệ.");
                        if (hi.Text.Trim().Length>0 && (!decimal.TryParse(hi.Text, out b) || b<0))
                        throw new BusinessException("Giá tối đa không hợp lệ.");
                        decimal? minimum = lo.Text.Trim().Length == 0?(decimal?)null:decimal.Parse(lo.Text);
                        decimal? maximum = hi.Text.Trim().Length == 0?(decimal?)null:decimal.Parse(hi.Text);
                        if (minimum.HasValue && maximum.HasValue && minimum>maximum)
                        throw new BusinessException("Giá từ phải nhỏ hơn hoặc bằng giá đến.");
                        minFilter = minimum; maxFilter = maximum; dlg.DialogResult = DialogResult.OK;
                    }); if (dlg.ShowDialog(this) == DialogResult.OK)Reload();
                }
            });
            var lifecycle = Theme.Button("Vòng đời hiện vật", 220, 1190, 210);
            Controls.Add(lifecycle);
            lifecycle.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn hiện vật.");
                }
                using (var dialog = new Form
                {
                    Text = "Vòng đời: "+Current.ItemCode, Width = 780, Height = 575, 
                    StartPosition = FormStartPosition.CenterParent
                })
                {
                    Theme.Apply(dialog);
                    var data = Theme.Grid(15, 18, 725, 485); data.DataSource = service.ItemJourney(Actor, Current.ItemId);
                    data.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
                    dialog.Controls.Add(data); dialog.ShowDialog(this);
                }
            });
            reject = Theme.Button("Từ chối", 440, 1190, 150);
            reject.Enabled = flags.Edit;
            Controls.Add(reject);
            reject.Click+=(s, e) => SetAppraisal("Rejected");
            needs = Theme.Button("Yêu cầu bổ sung", 605, 1190, 195);
            needs.Enabled = flags.Edit;
            Controls.Add(needs);
            needs.Click+=(s, e) => SetAppraisal("NeedInfo");
            var print = Theme.Button("In nhãn hiện vật", 813, 1190, 180);
            print.Enabled = flags.Print;
            Controls.Add(print);
            print.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn hiện vật.");
                }
                ReportForm.OpenItemLabel(this, Actor, Current.ItemId);
            });
            MoveActions(1270);
            Height = 1440;
            appraise = Theme.Button("Duyệt thẩm định", 810, 1270, 155);
            Controls.Add(appraise);
            appraise.Enabled = flags.Edit;
            appraise.Click+=(s, e) => SetAppraisal("Approved");
            var pictures = Theme.Button("Ảnh hiện vật", 790, 748, 160);
            Controls.Add(pictures);
            pictures.Click+=(s, e) => Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Hãy lưu và chọn hiện vật trước khi thêm ảnh.");
                }
                using (var form = new ItemImagesForm(Actor, Current.ItemId, Flags))form.ShowDialog(this);
            });
            Shown+=(s, e) => Theme.Safe(() => 
            {
                category.DataSource = service.Categories(Actor, "", "ITEMS", true);
                category.DisplayMember = "CategoryName"; category.ValueMember = "CategoryId";
                owner.DataSource = service.ConsignorOptions(Actor, "ITEMS"); owner.DisplayMember = "FullName"; owner.ValueMember = "ConsignorId";
                var categories = service.Categories(Actor, "", "ITEMS");
                categoryFilter.Items.Add(new FilterOption
                {
                    Id = 0, Name = "Tất cả loại"
                });
                foreach (var item in categories)categoryFilter.Items.Add(new FilterOption
                {
                    Id = item.CategoryId, Name = item.CategoryName
                });
                categoryFilter.SelectedIndex = 0;
                ownerFilter.Items.Add(new FilterOption
                {
                    Id = 0, Name = "Tất cả chủ hàng"
                });
                foreach (var item in service.ConsignorOptions(Actor, "ITEMS"))
                ownerFilter.Items.Add(new FilterOption
                {
                    Id = item.ConsignorId, Name = item.FullName
                });
                ownerFilter.SelectedIndex = 0;
                if (presetCategory.HasValue)
                for (int i = 0; i<categoryFilter.Items.Count; i++)
                if (((FilterOption)categoryFilter.Items[i]).Id == presetCategory.Value)categoryFilter.SelectedIndex = i;
                Reload();
            });
        }
        sealed class FilterOption
        {
            public int Id
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }
            public override string ToString()
            {
                return Name;
            }
        }
        void SetAppraisal(string decision)
        {
            Theme.Safe(() => 
            {
                if (Current == null)
                {
                    throw new BusinessException("Chọn hiện vật để thẩm định.");
                }
                decimal? value = decision == "Approved"?(decimal?)AppraisalPriceInput.Parse(appraisal.Text):null;
                service.Appraise(Actor, Current.ItemId, value, decision, appraisalNotes.Text);
                ReloadCommitted();
                MessageBox.Show(decision == "Approved"?"Đã duyệt thẩm định thành công.":"Đã lưu kết quả thẩm định.", "Auction Arena");
            });
        }
        protected override void OnModeChanged(bool isEditing)
        {
            // Hàm có thể được gọi từ hàm tạo lớp cha khi control chưa được dựng.
            if (appraisal == null || appraise == null) return;
            bool pending = Current != null && (Current.ItemStatus == "PendingAppraisal" ||
                Current.ItemStatus == "NeedInfo" || Current.ItemStatus == "Rejected");
            bool allowed = !isEditing && pending && Flags.Edit;
            appraisal.ReadOnly = !allowed;
            appraise.Enabled = allowed;
            reject.Enabled = allowed;
            needs.Enabled = allowed;
            notesEntry.Enabled = allowed;
            appraisalHint.Text = isEditing?"Lưu hoặc hủy sửa hồ sơ trước khi thẩm định.":
                Current == null?"Lưu và chọn hiện vật trước khi thẩm định.":
                !Flags.Edit?"Tài khoản chưa có quyền thẩm định.":
                !pending?"Hiện vật đã duyệt hoặc đã chuyển sang nghiệp vụ khác.":
                "Nhập giá rồi bấm Duyệt thẩm định. VD: 15.000.000";
        }
        protected override object Fetch(string q)
        {
            return service.Items(Actor, q, 
            categoryFilter.SelectedItem is FilterOption && ((FilterOption)categoryFilter.SelectedItem).Id>0?
            (int?)((FilterOption)categoryFilter.SelectedItem).Id:null, 
            ownerFilter.SelectedItem is FilterOption && ((FilterOption)ownerFilter.SelectedItem).Id>0?
            (int?)((FilterOption)ownerFilter.SelectedItem).Id:null, 
            statusFilter.SelectedIndex<=0?null:statusFilter.Text, minFilter, maxFilter);
        }
        protected override void Fill(ItemEntity x)
        {
            code.Text = x.ItemCode;
            name.Text = x.ItemName;
            price.Text = x.EstimatePrice.ToString("0.##");
            appraisal.Text = x.AppraisedPrice.HasValue?x.AppraisedPrice.Value.ToString("0.##"):"";
            description.Text = x.Description;
            origin.Text = x.Origin;
            condition.Text = x.ConditionDescription;
            location.Text = x.WarehouseLocation;
            category.SelectedValue = x.CategoryId;
            owner.SelectedValue = x.ConsignorId;
            era.Text = x.Era;
            dimensions.Text = x.Dimensions;
            material.Text = x.Material;
            highPrice.Text = x.EstimatePriceHigh.HasValue?x.EstimatePriceHigh.Value.ToString("0.##"):"";
            appraisalNotes.Text = x.AppraisalNotes;
        }
        protected override void Clear()
        {
            foreach (var t in new[]
            {
                code, name, price, appraisal, description, origin, condition, location, era, dimensions, material, highPrice, appraisalNotes
            })t.Clear();
        }
        protected override void Delete()
        {
            service.DeleteItem(Actor, Current.ItemId);
        }
        protected override void Save()
        {
            service.SaveItem(Actor, new ItemEntity
            {
                ItemId = Current == null?0:Current.ItemId, ItemCode = code.Text, ItemName = name.Text, CategoryId = Id(category), ConsignorId = Id(owner), EstimatePrice = Money(price), Description = description.Text, Origin = origin.Text, ConditionDescription = condition.Text, Era = era.Text, Dimensions = dimensions.Text, Material = material.Text, 
                EstimatePriceHigh = string.IsNullOrWhiteSpace(highPrice.Text)?(decimal?)null:Money(highPrice)
            });
        }
    }
}
