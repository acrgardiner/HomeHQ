using System.ComponentModel.DataAnnotations;

namespace projectaardvarkx2.Contracts;

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