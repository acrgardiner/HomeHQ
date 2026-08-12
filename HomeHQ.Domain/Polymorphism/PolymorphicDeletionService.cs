namespace HomeHQ.Polymorphism;

public class PolymorphicDeletionService(IPolymorphicChildStore store)
{
    public Task DeleteChildrenOfAsync(Guid parentId, string parentType, CancellationToken cancellationToken = default)
        => store.DeleteByParentAsync(parentId, parentType, cancellationToken);
}
