namespace HomeHQ.DTOs;

// ── Auth / current user ──────────────────────────────────────────────────────

public record CurrentUserDto(string UserName, IReadOnlyList<string> Roles);

// ── Users ────────────────────────────────────────────────────────────────────

public record UserDto(
    string Id,
    string UserName,
    DateTime? LastLogin,
    IReadOnlyList<string> Roles);

public record CreateUserRequest(string UserName, string Password, bool IsAdmin);

public record UpdateUserRequest(string UserName, bool IsAdmin);

public record ResetPasswordRequest(string NewPassword);

// ── Admin dashboard ──────────────────────────────────────────────────────────

public record AdminDashboardDto(
    int UserCount,
    int AttachmentCount,
    float TotalAttachmentSize,
    IReadOnlyList<LargeAttachmentDto> LargestAttachments,
    IReadOnlyList<ExpiringWarrantyDto> WarrantiesExpiringSoon,
    IReadOnlyList<SoftDeletedCountDto> PendingPurge);

public record LargeAttachmentDto(
    Guid Id,
    string OriginFileName,
    float FileSize,
    Guid? ParentId,
    string? ParentName);

public record ExpiringWarrantyDto(
    Guid Id,
    string Name,
    DateTime? WarrantyExpiration,
    string? CategoryName,
    string? WarrantyTypeName);

public record SoftDeletedCountDto(string EntityType, int Count);

// ── Logs ─────────────────────────────────────────────────────────────────────

public record LogEntryDto(
    string Timestamp,
    string Level,
    string SourceContext,
    string Message,
    string Exception);
