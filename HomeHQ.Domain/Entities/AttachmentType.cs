
namespace HomeHQ.Entities;

public class AttachmentType : AuditableEntity, IEntity, IEntityNamed
{
    public string Name { get; set; }
    public bool Default { get; set; } = false;
}
