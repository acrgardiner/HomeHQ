using Microsoft.EntityFrameworkCore;
using projectaardvarkx2.Contracts;
using projectaardvarkx2.Data;
using System.Linq.Expressions;

namespace projectaardvarkx2.Repositories
{
    public interface IGenericRepository<T> where T : AuditableEntity, IEntity
    {
        Task<T?> GetByIdAsync(Guid id, List<Expression<Func<T, object>>>? includes = null);
        Task<IEnumerable<T>> GetAsync<TKey>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, TKey>>? orderBy = null,
            bool descending = false,
            int? skip = null,
            int? take = null, 
            List<Expression<Func<T, object>>>? includes = null
        );
        Task<IEnumerable<T>> GetAllAsync(List<Expression<Func<T, object>>>? includes = null);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveChangesAsync();

        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, bool includeDeleted = false);
        Task<TResult> AggregateAsync<TResult>(
            Expression<Func<T, bool>>? filter,
            Expression<Func<T, TResult>> selector,
            Func<IQueryable<TResult>, Task<TResult>> aggregateFunc
        );

        Task<Dictionary<string, int>> GetCountByPropertyAsync<TKey>(Expression<Func<T, TKey>> propertySelector, Expression<Func<T, bool>>? filter = null);
    }

    public class GenericRepository<T> : IGenericRepository<T> where T : AuditableEntity, IEntity
    {
        private readonly ApplicationDbContext _applicationContext;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _applicationContext = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetQueryable(bool includeDeleted = false)
        {
            var query = _dbSet.AsQueryable();
            if (!includeDeleted && typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
            {
                // Build a filter for soft delete
                var parameter = Expression.Parameter(typeof(T), "e");
                var deletedOnProp = Expression.PropertyOrField(parameter, "DeletedOn");
                var nullConstant = Expression.Constant(null, typeof(DateTime?));
                var body = Expression.Equal(deletedOnProp, nullConstant);
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }
            return query;
        }

        public async Task<T?> GetByIdAsync(Guid id, List<Expression<Func<T, object>>>? includes = null)
        {
            IQueryable<T> query = GetQueryable();

            if (includes != null && includes.Count > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<T>> GetAllAsync(List<Expression<Func<T, object>>>? includes = null)
        {
            IQueryable<T> query = GetQueryable();

            if (includes != null && includes.Count > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync<TKey>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, TKey>>? orderBy = null,
            bool descending = false,
            int? skip = null,
            int? take = null,
            List<Expression<Func<T, object>>>? includes = null
        )
        {
            IQueryable<T> query = GetQueryable();

            if (includes != null && includes.Count > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            if (skip.HasValue)
                query = query.Skip(skip.Value);

            if (take.HasValue)
                query = query.Take(take.Value);

            return await query.ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _applicationContext.SaveChangesAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, bool includeDeleted = false)
        {
            IQueryable<T> query = GetQueryable(includeDeleted);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeDeleted)
                return await query.IgnoreQueryFilters().CountAsync();
            else
                 return await query.CountAsync();
        }

        public async Task<TResult> AggregateAsync<TResult>(
            Expression<Func<T, bool>>? filter,
            Expression<Func<T, TResult>> selector,
            Func<IQueryable<TResult>, Task<TResult>> aggregateFunc
        )
        {
            IQueryable<T> query = GetQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var selectedQuery = query.Select(selector);

            return await aggregateFunc(selectedQuery);
        }

        public async Task<Dictionary<string, int>> GetCountByPropertyAsync<TKey>(Expression<Func<T, TKey>> propertySelector, Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = GetQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query
                .GroupBy(propertySelector)
                .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }
    }
}
