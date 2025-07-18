using projectaardvarkx2.Contracts;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.Entities
{
    public class Asset : AuditableEntity, IAggregateRoot
    {
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseAmount { get; set; }
        public Guid? WarrantyTypeId { get; set; }
        public virtual WarrantyType? WarrantyType { get; set; }
        public DateTime? WarrantyExpiration { get; set; }

        //public virtual IEnumerable<Attachment>? Attachments { get; set; } = new List<Attachment>();
    }
}
