using HomeHQ.Contracts;

namespace HomeHQ.Entities;

public abstract class AuditableEntity : AuditableEntity<Guid>
{
}

public abstract class AuditableEntity<T> : BaseEntity<T>, IAuditableEntity, ISoftDelete
{
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedOn { get; set; }

    internal bool IsDeleted => DeletedBy is not null;

    public void MarkDeleted(DateTime utcNow, string userId)
    {
        DeletedOn = utcNow;
        DeletedBy = userId;
    }

    public void MarkCreated(DateTime utcNow, string userId)
    {
        CreatedOn = utcNow;
        CreatedBy = userId;
        LastModifiedOn = utcNow;
        LastModifiedBy = userId;
    }

    public void MarkModified(DateTime utcNow, string userId)
    {
        LastModifiedOn = utcNow;
        LastModifiedBy = userId;
    }
}
