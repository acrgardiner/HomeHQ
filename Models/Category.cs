using HomeHQ.Contracts;

namespace HomeHQ.Entities;

public class Category : AuditableEntity, IEntity, IEntityNamed
{
    public string Name { get; set; }
    public string? Icon { get; set; }

    //public ICollection<Asset> Assets { get; set; }  // Navigation property (one-to-many relationship with Asset)

    //[NotMapped]
    //public List<Attachment> Attachments { get; set; } = [];

    public string Title => Icon + " " + Name;
}
