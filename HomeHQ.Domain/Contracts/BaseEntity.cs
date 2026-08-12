namespace HomeHQ.Entities;

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; set; } = default!;
    //public string Name { get; set; } = default!;
    //public string? Description { get; set; }
}
