using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace AuctionArena.Data.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        private readonly DbSet<T> _set;
        public EfRepository(AuctionArenaDbContext context) // Khởi tạo EfRepository. Nhận phụ thuộc/cấu hình ban đầu.
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            _set = context.Set<T>();
        }
        public IQueryable<T> Query() { return _set; } // Trả IQueryable để ghép truy vấn LINQ trước khi thực thi.
        public IQueryable<T> Where(Expression<Func<T, bool>> predicate) { return _set.Where(predicate); } // Lọc bằng biểu thức LINQ.
        public T Find(params object[] keys) { return _set.Find(keys); } // Tìm Entity theo khóa.
        public void Add(T entity) { _set.Add(entity); } // Thêm đối tượng/ảnh vào luồng lưu.
        public void Remove(T entity) { _set.Remove(entity); } // Xóa đối tượng/ảnh theo điều kiện.
    }
}
