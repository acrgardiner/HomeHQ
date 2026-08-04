namespace HomeHQ.Entities;

public class Asset : AuditableEntity, IEntity, IEntityNamed
{
    public string Name { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? PurchasedFrom { get; set; }
    public Guid? WarrantyTypeId { get; set; }
    public virtual WarrantyType? WarrantyType { get; set; }
    public DateTime? WarrantyExpiration { get; set; }

    //public virtual IEnumerable<Attachment>? Attachments { get; set; } = new List<Attachment>();
    //public virtual IEnumerable<AttributeValue>? Attributes { get; set; } = new List<AttributeValue>();
    //public virtual IEnumerable<Note>? Notes { get; set; } = new List<Note>();
}
