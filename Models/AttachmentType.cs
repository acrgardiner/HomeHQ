using projectaardvarkx2.Contracts;

namespace projectaardvarkx2.Entities;

public class AttachmentType : AuditableEntity, IEntity
{
    public bool Default { get; set; } = false;
}
