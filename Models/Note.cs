using HomeHQ.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHQ.Entities;

public class Note : AuditableEntity, IEntity
{
    public Guid? ParentId { get; set; }
    public string ParentType { get; set; } = string.Empty;
    [NotMapped]
    public virtual IEntityNamed? Parent { get; set; } // Navigation property for any entity
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public Note() { }

    public Note(Note note)
    {
        ParentType = note.ParentType;
        ParentId = note.ParentId;
        Title = note.Title;
        Content = note.Content;
    }

}
