using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using AuctionArena.Data;
using AuctionArena.Data.Models;
namespace AuctionArena.Services
{
    public sealed class ItemImageInfo
    {
        public int ItemImageId
        {
            get;
            set;
        }
        public string RelativePath
        {
            get;
            set;
        }
        public bool IsPrimary
        {
            get;
            set;
        }
    }
    public sealed class ItemImageService : ServiceBase
    {
        public ItemImageService(Func<AuctionArenaDbContext> factory = null) : base(factory)
        {
        }
        // Ảnh lưu trong AppData của tài khoản Windows; không mất khi Clean/Rebuild solution.
        // Các máy khác cần đồng bộ thư mục ảnh hoặc dùng vùng lưu chung.
        public static string ImagesDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuctionArena", "Images");
            }
        }
        public static string FilePath(string relativePath) //Ghép/kiểm tra đường dẫn ảnh trong thư mục được phép
        {
            var name = Path.GetFileName(relativePath ?? "");
            if (string.IsNullOrWhiteSpace(name) || name != relativePath)
            throw new BusinessException("Đường dẫn ảnh không hợp lệ.");
            return Path.Combine(ImagesDirectory, name);
        }
        public List<ItemImageInfo> Get(int actor, int itemId) //Lấy dữ liệu theo điều kiện của Service.
        {
            using (var db = Factory())
            {
                SecurityService.Require(db, actor, "ITEMS", "View");
                return db.ItemImages.AsNoTracking().Where(x => x.ItemId == itemId)
                .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.ItemImageId)
                .Select(x => new ItemImageInfo
                {
                    ItemImageId = x.ItemImageId, 
                    RelativePath = x.RelativePath, IsPrimary = x.IsPrimary
                }).ToList();
            }
        }
        public void Add(int actor, int itemId, string source) //Thêm đối tượng/ảnh vào luồng lưu(63-113).
        {
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            throw new BusinessException("Không tìm thấy file ảnh.");
            var extension = Path.GetExtension(source).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            throw new BusinessException("Chỉ nhận PNG/JPG/JPEG.");
            if (new FileInfo(source).Length > 8 * 1024 * 1024)
            throw new BusinessException("Ảnh không được vượt quá 8 MB.");
            // Kiểm tra dữ liệu thực là ảnh; ngăn file giả đuôi .jpg.
            using (var check = System.Drawing.Image.FromFile(source))
            if (check.Width < 1 || check.Height < 1)
            throw new BusinessException("Ảnh không hợp lệ.");
            string relative = Guid.NewGuid().ToString("N") + extension;
            Directory.CreateDirectory(ImagesDirectory);
            string destination = FilePath(relative);
            File.Copy(source, destination, false);
            try
            {
                using (var db = Factory())
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        SecurityService.Require(db, actor, "ITEMS", "Edit");
                        var item = db.Items.SingleOrDefault(x => x.ItemId == itemId);
                        if (item == null || (item.ItemStatus != "PendingAppraisal" && item.ItemStatus != "NeedInfo" && 
                        item.ItemStatus != "Approved" && item.ItemStatus != "InStock"))
                        throw new BusinessException("Chỉ bổ sung ảnh khi hiện vật đang thẩm định hoặc trong kho.");
                        bool primary = !db.ItemImages.Any(x => x.ItemId == itemId);
                        db.ItemImages.Add(new ItemImageEntity
                        {
                            ItemId = itemId, RelativePath = relative, IsPrimary = primary
                        });
                        Audit(db, actor, "ITEM_IMAGE_ADD", "Items", itemId, "Bổ sung ảnh hiện vật");
                        db.SaveChanges();
                        tx.Commit();
                    }
                    catch
                    {
                        RollbackSafely(tx);
                        throw;
                    }
                }
            }
            catch
            {
                if (File.Exists(destination)) File.Delete(destination);
                throw;
            }
        }
        // Chỉ một ảnh đại diện; transaction tránh hai ảnh cùng IsPrimary.
        public void SetPrimary(int actor, int imageId) //Đặt ảnh chính và bỏ cờ ảnh chính khác(115-145)
        {
            using (var db = Factory())using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    SecurityService.Require(db, actor, "ITEMS", "Edit");
                    var photo = db.ItemImages.SingleOrDefault(x => x.ItemImageId == imageId);
                    if (photo == null)
                    {
                        throw new BusinessException("Không tìm thấy ảnh hiện vật.");
                    }
                    var item = db.Items.Single(x => x.ItemId == photo.ItemId);
                    if (!new[]
                    {
                        "PendingAppraisal", "NeedInfo", "Approved", "InStock"
                    }.Contains(item.ItemStatus))
                    throw new BusinessException("Hiện vật đang đấu giá/đã giao: không đổi ảnh chính.");
                    foreach (var image in db.ItemImages.Where(x => x.ItemId == photo.ItemId).ToList())
                    image.IsPrimary = image.ItemImageId == imageId;
                    Audit(db, actor, "ITEM_IMAGE_PRIMARY", "Items", item.ItemId, "Chọn ảnh chính #"+imageId);
                    db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
        }
        public void Remove(int actor, int imageId) //Xóa đối tượng/ảnh theo điều kiện(146-191).
        {
            string path = null;
            using (var db = Factory())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    SecurityService.Require(db, actor, "ITEMS", "Edit");
                    var photo = db.ItemImages.SingleOrDefault(x => x.ItemImageId == imageId);
                    if (photo == null)
                    {
                        throw new BusinessException("Không tìm thấy ảnh.");
                    }
                    var item = db.Items.Single(x => x.ItemId == photo.ItemId);
                    if (item.ItemStatus != "PendingAppraisal" && item.ItemStatus != "NeedInfo" && 
                    item.ItemStatus != "Approved" && item.ItemStatus != "InStock")
                    throw new BusinessException("Không được xóa ảnh hiện vật đã đấu giá.");
                    path = FilePath(photo.RelativePath);
                    db.ItemImages.Remove(photo);
                    if (photo.IsPrimary)
                    {
                        var replacement = db.ItemImages.Where(x => x.ItemId == photo.ItemId && x.ItemImageId != imageId)
                        .OrderBy(x => x.ItemImageId).FirstOrDefault();
                        if (replacement != null) replacement.IsPrimary = true;
                    }
                    Audit(db, actor, "ITEM_IMAGE_REMOVE", "Items", item.ItemId, "Gỡ ảnh hiện vật");
                    db.SaveChanges();
                    tx.Commit();
                }
                catch
                {
                    RollbackSafely(tx);
                    throw;
                }
            }
            // Chỉ xóa ảnh sau khi thay đổi dữ liệu đã được lưu thành công.
            try
            {
                if (path != null && File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
                // Tránh mất bản ghi giao dịch chỉ vì file đang được xem.
            }
        }
    }
}
