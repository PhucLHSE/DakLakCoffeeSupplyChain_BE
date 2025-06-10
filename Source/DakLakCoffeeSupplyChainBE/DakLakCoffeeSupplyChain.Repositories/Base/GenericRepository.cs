using DakLakCoffeeSupplyChain.Repositories.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Base
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected DakLakCoffee_SCMContext _context;

        public GenericRepository()
        {
            _context ??= new DakLakCoffee_SCMContext();
        }

        public GenericRepository(DakLakCoffee_SCMContext context)
        {
            _context = context;
        }

        public List<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<List<T>> GetAllAsync(
            Func<IQueryable<T>, IQueryable<T>> include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            bool asNoTracking = true
        )
        {
            IQueryable<T> query = _context.Set<T>();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            if (orderBy != null)
                query = orderBy(query);

            return await query.ToListAsync();
        }

        public void Create(T entity)
        {
            _context.Add(entity);
            //_context.SaveChanges();
        }

        public async Task CreateAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        //public async Task<int> CreateAsync(T entity)
        //{
        //    _context.Add(entity);

        //    return await _context.SaveChangesAsync();
        //}

        public void Update(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            //_context.SaveChanges();
        }

        public async Task UpdateAsync(T entity)
        {
            await Task.Yield();                     // để tránh warning "method lacks 'await'"
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        //public async Task<int> UpdateAsync(T entity)
        //{
        //    var tracker = _context.Attach(entity);
        //    tracker.State = EntityState.Modified;

        //    return await _context.SaveChangesAsync();
        //}

        public bool Remove(T entity)
        {
            _context.Remove(entity);
            _context.SaveChanges();

            return true;
        }

        public async Task RemoveAsync(T entity)
        {
            await Task.Yield();
            _context.Remove(entity);
        }

        //public async Task<bool> RemoveAsync(T entity)
        //{
        //    _context.Remove(entity);
        //    await _context.SaveChangesAsync();

        //    return true;
        //}

        public T GetById(int id)
        {
            var entity = _context.Set<T>().Find(id);

            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);

            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public T GetById(string code)
        {
            var entity = _context.Set<T>().Find(code);

            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public async Task<T> GetByIdAsync(string code)
        {
            var entity = await _context.Set<T>().FindAsync(code);

            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public T GetById(Guid code)
        {
            var entity = _context.Set<T>().Find(code);

            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public async Task<T> GetByIdAsync(Guid code)
        {
            var entity = await _context.Set<T>().FindAsync(code);

            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public async Task<T?> GetByIdAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            bool asNoTracking = true
        )
        {
            IQueryable<T> query = _context.Set<T>();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            return await query.FirstOrDefaultAsync(predicate); // EF dịch được thành SQL
        }


        #region Separating asigned entity and save operators        

        public void PrepareCreate(T entity)
        {
            _context.Add(entity);
        }

        public void PrepareUpdate(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        public void PrepareRemove(T entity)
        {
            _context.Remove(entity);
        }

        public int Save()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion Separating asign entity and save operators

        public IQueryable<T> GetAllQueryable()
        {
            return _context.Set<T>().AsQueryable();
        }
    }
}
