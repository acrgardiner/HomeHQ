using projectaardvarkx2.Contracts;

namespace projectaardvarkx2.Entities;

public class AttachmentType : AuditableEntity, IEntity, IEntityNamed
{
    public string Name { get; set; }
    public bool Default { get; set; } = false;
}
