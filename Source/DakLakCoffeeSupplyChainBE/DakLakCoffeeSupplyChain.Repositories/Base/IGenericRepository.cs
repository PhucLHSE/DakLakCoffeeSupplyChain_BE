using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Base
{
    public interface IGenericRepository<T> where T : class
    {
        List<T> GetAll();

        Task<List<T>> GetAllAsync();

        IQueryable<T> GetAllQueryable();

        T GetById(int id);

        Task<T> GetByIdAsync(int id);

        T GetById(string code);

        Task<T> GetByIdAsync(string code);

        T GetById(Guid id);

        Task<T> GetByIdAsync(Guid id);

        Task<int> CreateAsync(T entity);

        Task<int> UpdateAsync(T entity);

        Task<bool> RemoveAsync(T entity);

        void PrepareCreate(T entity);

        void PrepareUpdate(T entity);

        void PrepareRemove(T entity);

        int Save();

        Task<int> SaveAsync();
    }
}
