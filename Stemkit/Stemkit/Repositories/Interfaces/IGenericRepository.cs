using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Stemkit.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Synchronous methods
        T Get(Expression<Func<T, bool>> predicate);
        IEnumerable<T> GetAll();
        T GetById(int id);
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void Delete(T entity);
        void DeleteById(int id);
        void Save();

        // Asynchronous methods
        Task<T> GetAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task SaveAsync();
    }
}

