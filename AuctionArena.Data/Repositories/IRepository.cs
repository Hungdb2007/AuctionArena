using System;
using System.Linq;
using System.Linq.Expressions;

namespace AuctionArena.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        // Hợp đồng generic cho truy vấn và thao tác đối tượng dữ liệu.
        IQueryable<T> Query(); // Khai báo hợp đồng/điểm mở rộng; lớp triển khai cung cấp xử lý ( Query. Find, Add, Remove, Where )
        T Find(params object[] keys);
        void Add(T entity);
        void Remove(T entity);
        IQueryable<T> Where(Expression<Func<T, bool>> predicate);
    }
}
