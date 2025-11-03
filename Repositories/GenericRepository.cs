using Microsoft.EntityFrameworkCore;
using projectaardvarkx2.Contracts;
using projectaardvarkx2.Data;
using projectaardvarkx2.Entities;
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

            bool includeParent = false;

            if (includes != null && includes.Count > 0)
            {
                includeParent = includes.RemoveAll(e => e.ToString().Contains("Parent")) > 0;

                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            var result = await query.FirstOrDefaultAsync(e => e.Id == id);

            if (includeParent && result != null)
            {
                await LoadPolymorphicParents(new[] { result });
            }

            return result;
        }

        public async Task<IEnumerable<T>> GetAllAsync(List<Expression<Func<T, object>>>? includes = null)
        {
            IQueryable<T> query = GetQueryable();

            bool includeParent = false;

            if (includes != null && includes.Count > 0)
            {
                includeParent = includes.RemoveAll(e => e.ToString().Contains("Parent")) > 0;

                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            var result = await query.ToListAsync();

            if (includeParent)
            {
                await LoadPolymorphicParents(result);
            }

            return result;
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

            bool includeParent = false;

            if (includes != null && includes.Count > 0)
            {
                includeParent = includes.RemoveAll(e => e.ToString().Contains("Parent")) > 0;

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

            var result = await query.ToListAsync();

            if (includeParent)
            {
                await LoadPolymorphicParents(result);
            }

            return result;
        }

        private async Task LoadPolymorphicParents(IEnumerable<T> entities)
        {
            if (!entities.Any()) return;

            // Group entities by ParentType for efficient querying
            var parentGroups = entities
                .GroupBy(e => GetParentType(e))
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToList();

            foreach (var group in parentGroups)
            {
                var parentType = group.Key;
                var parentIds = group
                    .Select(e => GetParentId(e))
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                if (!parentIds.Any()) continue;

                // Load parents based on type
                var parents = await LoadParentsByType(parentType, parentIds);

                // Assign parents to entities
                foreach (var entity in group)
                {
                    var parentId = GetParentId(entity);
                    if (parentId.HasValue)
                    {
                        var parent = parents.FirstOrDefault(p => GetEntityId(p) == parentId.Value);
                        SetParent(entity, parent);
                    }
                }
            }
        }

        private string? GetParentType(T entity)
        {
            return entity.GetType().GetProperty("ParentType")?.GetValue(entity)?.ToString();
        }

        private Guid? GetParentId(T entity)
        {
            return entity.GetType().GetProperty("ParentId")?.GetValue(entity) as Guid?;
        }

        private void SetParent(T entity, object? parent)
        {
            var parentProperty = entity.GetType().GetProperty("Parent");
            parentProperty?.SetValue(entity, parent);
        }

        private Guid GetEntityId(object entity)
        {
            var idProperty = entity.GetType().GetProperty("Id");
            return (Guid)(idProperty?.GetValue(entity) ?? Guid.Empty);
        }

        private async Task<List<object>> LoadParentsByType(string parentType, List<Guid> parentIds)
        {
            return parentType switch
            {
                nameof(Asset) => (await _applicationContext.Assets
                    .Where(a => parentIds.Contains(a.Id))
                    .ToListAsync()).Cast<object>().ToList(),

                nameof(Category) => (await _applicationContext.Categories
                    .Where(c => parentIds.Contains(c.Id))
                    .ToListAsync()).Cast<object>().ToList(),

                nameof(WarrantyType) => (await _applicationContext.WarrantyTypes
                    .Where(w => parentIds.Contains(w.Id))
                    .ToListAsync()).Cast<object>().ToList(),

                nameof(AttachmentType) => (await _applicationContext.AttachmentTypes
                    .Where(at => parentIds.Contains(at.Id))
                    .ToListAsync()).Cast<object>().ToList(),

                _ => new List<object>()
            };
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
            // Delete polymorphic children first
            DeletePolymorphicChildren(entity.Id);

            _dbSet.Remove(entity);
        }

        private void DeletePolymorphicChildren(Guid parentId)
        {
            // Get the parent type name
            var parentType = typeof(T).Name;

            // Delete all Attachments linked to this parent
            var attachments = _applicationContext.Attachments
                .Where(a => a.ParentId == parentId && a.ParentType == parentType)
                .ToList();

            if (attachments.Any())
            {
                _applicationContext.Attachments.RemoveRange(attachments);
            }

            // Delete all Notes linked to this parent
            var notes = _applicationContext.Notes
                .Where(n => n.ParentId == parentId && n.ParentType == parentType)
                .ToList();

            if (notes.Any())
            {
                _applicationContext.Notes.RemoveRange(notes);
            }

            // Delete all AttributeValues linked to this parent
            var attributes = _applicationContext.Attributes
                .Where(a => a.ParentId == parentId && a.ParentType == parentType)
                .ToList();

            if (attributes.Any())
            {
                _applicationContext.Attributes.RemoveRange(attributes);
            }
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
