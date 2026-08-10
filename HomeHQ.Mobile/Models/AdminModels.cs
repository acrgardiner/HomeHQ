namespace HomeHQ.Mobile.Models;

public class UserListItem
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public DateTime? LastLogin { get; init; }
    public string LastLoginDisplay { get; init; } = string.Empty;
    public string RolesDisplay { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string Initial => string.IsNullOrEmpty(UserName) ? "?" : UserName[0].ToString().ToUpperInvariant();
}

public class SoftDeletedCountItem
{
    public required string EntityType { get; init; }
    public int Count { get; init; }
}

public class LargeAttachmentItem
{
    public Guid Id { get; init; }
    public required string OriginFileName { get; init; }
    public string FileSizeDisplay { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public string ParentName { get; init; } = string.Empty;
}

public class ExpiringWarrantyItem
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string WarrantyExpirationDisplay { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string WarrantyTypeName { get; init; } = string.Empty;
}

public class LogEntryItem
{
    public required string Timestamp { get; init; }
    public required string Level { get; init; }
    public required string SourceContext { get; init; }
    public required string Message { get; init; }
    public string Exception { get; init; } = string.Empty;
    public bool HasException => !string.IsNullOrWhiteSpace(Exception);
    public Color LevelColor => Level switch
    {
        "Error" or "Fatal" => Colors.Red,
        "Warning" => Colors.Orange,
        "Information" => Colors.SteelBlue,
        "Debug" or "Verbose" => Colors.Gray,
        _ => Colors.Gray
    };
}
