namespace HomeHQ.Entities;

public interface IPolymorphicEntity
{
    public Guid? ParentId { get; set; }
    public string ParentType { get; set; }
}
