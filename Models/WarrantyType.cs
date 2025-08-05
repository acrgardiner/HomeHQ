using projectaardvarkx2.Contracts;
using System.ComponentModel.DataAnnotations;

namespace projectaardvarkx2.Entities;

public class WarrantyType : AuditableEntity, IEntity, IEntityNamed
{
    public string Name { get; set; }
    public int? Days { get; set; }
    public int? Months { get; set; }
    public int? Years { get; set; }
    public bool Default { get; set; } = false;

    //[NotMapped]
    //public List<Attachment> Attachments { get; set; } = [];

    public int? SortOrder => (Days ?? 0) + (Months * 30 ?? 0) + (Years * 365 ?? 0);
}
