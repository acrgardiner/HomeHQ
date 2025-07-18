using System.ComponentModel.DataAnnotations;

namespace projectaardvarkx2.Contracts;

public interface IEntity
{
}

public interface IEntity<TId> : IEntity
{
    TId Id { get; }
    string Name { get; }
    //string Description { get; }
}