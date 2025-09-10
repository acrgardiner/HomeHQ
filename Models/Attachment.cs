using projectaardvarkx2.Contracts;

namespace projectaardvarkx2.Entities;

public class Attachment : AuditableEntity, IEntity
{
    public Guid? ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    public virtual Asset? Parent { get; set; }
    public Guid? AttachmentTypeId { get; set; }
    public virtual AttachmentType? AttachmentType { get; set; }

    public string LocalFileName { get; set; } = string.Empty;
    public string OriginFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public float FileSize { get; set; }

    public Attachment() { }

    public Attachment(Attachment attachment)
    {
        ParentType = attachment.ParentType;
        ParentId = attachment.ParentId;
        AttachmentTypeId = attachment.AttachmentTypeId;
        OriginFileName = attachment.OriginFileName;
        ContentType = attachment.ContentType;
        Extension = attachment.Extension;
    }
}

