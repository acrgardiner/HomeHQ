using System.Linq.Expressions;
using HomeHQ.Entities;

namespace HomeHQ.Services;

public interface IEntityService<T> where T : IAuditableEntity, IEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(List<Expression<Func<T, object>>>? includes = null);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? filter = null, Expression<Func<T, object>>? orderby = null, bool descending = false, int? skip = null, int? take = null, List<Expression<Func<T, object>>>? includes = null);
    Task<T?> GetByIdAsync(Guid id, List<Expression<Func<T, object>>>? includes = null);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities);
    Task DeleteAsync(Guid id);

    Task<TResult> AggregateAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<TResult>, Task<TResult>> aggregateFunc
    );

    Task<Dictionary<string, int>> GetCountByPropertyAsync<TKey>(Expression<Func<T, TKey>> propertySelector, Expression<Func<T, bool>>? filter = null);

    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, bool includeDeleted = false);
    Task<int> SumAsync(Expression<Func<T, int>> selector, Expression<Func<T, bool>>? filter = null);
    Task<long> SumAsync(Expression<Func<T, long>> selector, Expression<Func<T, bool>>? filter = null);
    Task<float> SumAsync(Expression<Func<T, float>> selector, Expression<Func<T, bool>>? filter = null);
    Task<double> SumAsync(Expression<Func<T, double>> selector, Expression<Func<T, bool>>? filter = null);
    Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, Expression<Func<T, bool>>? filter = null);
}
