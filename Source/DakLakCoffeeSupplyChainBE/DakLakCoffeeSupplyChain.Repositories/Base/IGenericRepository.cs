using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Base
{
    public interface IGenericRepository<T> where T : class
    {
        List<T> GetAll();

        Task<List<T>> GetAllAsync();

        Task<List<T>> GetAllAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = true
        );

        IQueryable<T> GetAllQueryable();

        T GetById(int id);

        Task<T> GetByIdAsync(int id);

        T GetById(string code);

        Task<T> GetByIdAsync(string code);

        T GetById(Guid id);

        Task<T> GetByIdAsync(Guid id);

        Task<T?> GetByIdAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true
        );

        Task CreateAsync(T entity);

        Task BulkCreateAsync(IEnumerable<T> entities);

        //Task<int> CreateAsync(T entity);

        Task UpdateAsync(T entity);

        //Task<int> UpdateAsync(T entity);

        Task RemoveAsync(T entity);

        //Task<bool> RemoveAsync(T entity);

        void PrepareCreate(T entity);

        void PrepareUpdate(T entity);

        void PrepareRemove(T entity);

        int Save();

        Task<int> SaveAsync();

        Task<bool> AnyAsync(
            Expression<Func<T, bool>> predicate,
            bool asNoTracking = true
        );

        Task<TResult?> GetByPredicateAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = true
        );
    }
}
