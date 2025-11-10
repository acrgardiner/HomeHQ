namespace HomeHQ.Contracts;

public interface IEntity
{
}

public interface IEntity<TId> : IEntity
{
    TId Id { get; }
}

public interface IEntityNamed
{
    string Name { get; set; }
}