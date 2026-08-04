using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHQ.Entities;

public class Attachment : AuditableEntity, IEntity, IPolymorphicEntity
{
    public Guid? ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    [NotMapped]
    public virtual IEntityNamed? Parent { get; set; }
    public Guid? AttachmentTypeId { get; set; }
    public virtual AttachmentType? AttachmentType { get; set; }

    public string LocalFileName { get; set; } = string.Empty;
    public string OriginFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public float FileSize { get; set; }

    public string Thumb_LocalFileName { get; set; } = string.Empty;
    public string Thumb_ContentType { get; set; } = string.Empty;
    public string Thumb_Extension { get; set; } = string.Empty;
    public float Thumb_FileSize { get; set; }

    /// <summary>Client-only: raw file bytes for attachments not yet uploaded (not persisted server-side).</summary>
    [NotMapped]
    public byte[]? PendingUploadBytes { get; set; }

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

