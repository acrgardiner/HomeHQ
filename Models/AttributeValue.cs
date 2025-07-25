using projectaardvarkx2.Contracts;

namespace projectaardvarkx2.Entities;

public class AttributeValue : AuditableEntity, IEntity
{
    public Guid? ParentId { get; set; }
    public string? Value { get; set; }
}
