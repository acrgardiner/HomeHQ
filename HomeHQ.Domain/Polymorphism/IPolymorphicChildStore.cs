namespace HomeHQ.Polymorphism;

public interface IPolymorphicChildStore
{
    /// <summary>
    /// Soft-deletes all polymorphic children (attachments, notes, attributes) for the given parent.
    /// </summary>
    Task DeleteByParentAsync(Guid parentId, string parentType, CancellationToken cancellationToken = default);
}
