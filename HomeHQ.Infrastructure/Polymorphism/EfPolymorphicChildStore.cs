using HomeHQ.Data;
using HomeHQ.Entities;
using HomeHQ.Polymorphism;
using Microsoft.EntityFrameworkCore;

namespace HomeHQ.Infrastructure.Polymorphism;

public class EfPolymorphicChildStore : IPolymorphicChildStore
{
    private readonly ApplicationDbContext _db;

    public EfPolymorphicChildStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task DeleteByParentAsync(Guid parentId, string parentType, CancellationToken cancellationToken = default)
    {
        if (parentId == Guid.Empty)
        {
            throw new ArgumentException("Parent id must be a non-empty GUID.", nameof(parentId));
        }

        if (string.IsNullOrWhiteSpace(parentType))
        {
            throw new ArgumentException("Parent type is required.", nameof(parentType));
        }

        await SoftDeleteSet(_db.Attachments, parentId, parentType, cancellationToken);
        await SoftDeleteSet(_db.Notes, parentId, parentType, cancellationToken);
        await SoftDeleteSet(_db.Attributes, parentId, parentType, cancellationToken);
    }

    private static async Task SoftDeleteSet<T>(
        DbSet<T> set,
        Guid parentId,
        string parentType,
        CancellationToken cancellationToken)
        where T : class, IPolymorphicEntity, ISoftDelete
    {
        var items = await set
            .Where(x => x.ParentId == parentId && x.ParentType == parentType && x.DeletedOn == null)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            // SaveChanges interceptor converts Deleted -> soft delete (Modified + DeletedOn).
            set.Remove(item);
        }
    }
}
