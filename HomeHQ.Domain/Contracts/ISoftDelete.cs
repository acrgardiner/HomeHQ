namespace HomeHQ.Entities;

public interface ISoftDelete
{
    DateTime? DeletedOn { get; set; }
    string? DeletedBy { get; set; }

    bool IsDeleted => DeletedOn.HasValue;
}
