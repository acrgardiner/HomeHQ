using HomeHQ.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHQ.Entities;

public class AttributeValue : AuditableEntity, IEntity
{
    public Guid? ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    [NotMapped]
    public virtual IEntityNamed? Parent { get; set; } // Navigation property for any entity
    public string? Attribute { get; set; }
    public string? Value { get; set; }
}
