using HomeHQ.Entities;
using HomeHQ.Polymorphism;
using HomeHQ.Repositories;

namespace HomeHQ.Application.Polymorphism;

/// <summary>
/// Shared delete orchestration for entities that may own polymorphic children.
/// Used by <see cref="Services.EntityService{T}"/> and API controllers.
/// </summary>
public static class PolymorphicEntityDeletion
{
    public static async Task DeleteAsync<T>(
        PolymorphicDeletionService polymorphicDeletion,
        IGenericRepository<T> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where T : AuditableEntity, IEntity
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity is null)
        {
            return;
        }

        if (PolymorphicParentTypes.IsPolymorphicParent(typeof(T).Name))
        {
            await polymorphicDeletion.DeleteChildrenOfAsync(id, typeof(T).Name, cancellationToken);
        }

        repository.Delete(entity);
        await repository.SaveChangesAsync();
    }
}
