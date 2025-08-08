using projectaardvarkx2.Contracts;

namespace projectaardvarkx2.Entities;

public class Attachment : AuditableEntity, IEntity
{
    public Guid? ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    public virtual Asset? Parent { get; set; } = default;
    public Guid? AttachmentTypeId { get; set; }
    public virtual AttachmentType? AttachmentType { get; set; } = default;

    public string LocalFileName { get; set; } = string.Empty;
    public string OriginFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public float FileSize { get; set; } = 0;
}

