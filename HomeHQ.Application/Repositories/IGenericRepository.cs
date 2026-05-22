using HomeHQ.Contracts;
using HomeHQ.Entities;
using System.Linq.Expressions;

namespace HomeHQ.Repositories;

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
