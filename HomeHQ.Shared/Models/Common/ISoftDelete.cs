namespace HomeHQ.Contracts;

public interface ISoftDelete
{
    DateTime? DeletedOn { get; set; }
    string? DeletedBy { get; set; }

    bool IsDeleted => DeletedOn != null;
}