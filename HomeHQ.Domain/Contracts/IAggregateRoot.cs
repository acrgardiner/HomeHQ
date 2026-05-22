namespace HomeHQ.Contracts;

// Apply this marker interface only to aggregate root entities (top level)
// Repositories will only work with aggregate roots, not their children
public interface IAggregateRoot : IEntity
{
}