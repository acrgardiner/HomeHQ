using HomeHQ.Application.Polymorphism;
using HomeHQ.Entities;
using HomeHQ.Polymorphism;
using HomeHQ.Repositories;
using System.Linq.Expressions;

namespace HomeHQ.Services;

public class EntityService<T> : IEntityService<T> where T : AuditableEntity, IEntity
{
    private readonly IGenericRepository<T> _repository;
    private readonly PolymorphicDeletionService _polymorphicDeletion;

    public EntityService(IGenericRepository<T> repository, PolymorphicDeletionService polymorphicDeletion)
    {
        _repository = repository;
        _polymorphicDeletion = polymorphicDeletion;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderby = null,
        bool descending = false,
        int? skip = null,
        int? take = null,
        List<Expression<Func<T, object>>>? includes = null
    )
    {
        return await _repository.GetAsync(filter, orderby, descending, skip, take, includes);
    }

    public async Task<IEnumerable<T>> GetAllAsync(List<Expression<Func<T, object>>>? includes = null)
    {
        return await _repository.GetAllAsync(includes);
    }

    public async Task<T?> GetByIdAsync(Guid id, List<Expression<Func<T, object>>>? includes = null)
    {
        return await _repository.GetByIdAsync(id, includes);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        return entity;
    }

    public async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            _repository.Update(entity);
        }

        await _repository.SaveChangesAsync();

        return entityList;
    }

    public Task DeleteAsync(Guid id) =>
        PolymorphicEntityDeletion.DeleteAsync(_polymorphicDeletion, _repository, id);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, bool includeDeleted = false)
    {
        return await _repository.CountAsync(filter, includeDeleted);
    }

    public async Task<TResult> AggregateAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<TResult>, Task<TResult>> aggregateFunc
    )
    {
        return await _repository.AggregateAsync(filter, selector, aggregateFunc);
    }

    public async Task<Dictionary<string, int>> GetCountByPropertyAsync<TKey>(Expression<Func<T, TKey>> propertySelector, Expression<Func<T, bool>>? filter = null)
    {
        return await _repository.GetCountByPropertyAsync(propertySelector, filter);
    }

    public Task<int> SumAsync(Expression<Func<T, int>> selector, Expression<Func<T, bool>>? filter = null)
    {
        return _repository.AggregateAsync(
            filter,
            selector,
            async query => await Task.FromResult(query.Sum())
        );
    }

    public Task<long> SumAsync(Expression<Func<T, long>> selector, Expression<Func<T, bool>>? filter = null)
    {
        return _repository.AggregateAsync(
            filter,
            selector,
            async query => await Task.FromResult(query.Sum())
        );
    }

    public Task<float> SumAsync(Expression<Func<T, float>> selector, Expression<Func<T, bool>>? filter = null)
    {
        return _repository.AggregateAsync(
            filter,
            selector,
            async query => await Task.FromResult(query.Sum())
        );
    }

    public Task<double> SumAsync(Expression<Func<T, double>> selector, Expression<Func<T, bool>>? filter = null)
    {
        return _repository.AggregateAsync(
            filter,
            selector,
            async query => await Task.FromResult(query.Sum())
        );
    }

    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, Expression<Func<T, bool>>? filter = null)
    {
        return _repository.AggregateAsync(
            filter,
            selector,
            async query => await Task.FromResult(query.Sum())
        );
    }
}
