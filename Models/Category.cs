using projectaardvarkx2.Contracts;

namespace projectaardvarkx2.Entities
{
    public class Category : AuditableEntity, IAggregateRoot
    {
        public string? Icon { get; set; }

        //public ICollection<Asset> Assets { get; set; }  // Navigation property (one-to-many relationship with Asset)

        //[NotMapped]
        //public List<Attachment> Attachments { get; set; } = [];

        public string Title => Icon + " " + Name;
    }
}
