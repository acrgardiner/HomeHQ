namespace HomeHQ.DTOs;

public interface IUpdateRequest
{
    Guid Id { get; }
}

// ── Read models (API responses) ─────────────────────────────────────────────

public record AuditableDto(Guid Id, DateTime? CreatedOn, DateTime? LastModifiedOn) : IEntityDto;

public record CategoryDto(Guid Id, string Name, string? Icon, DateTime? CreatedOn = null, DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn)
{
    public string Title => $"{Icon} {Name}".Trim();
}

public record WarrantyTypeDto(
    Guid Id,
    string Name,
    int? Days,
    int? Months,
    int? Years,
    bool Default,
    int? SortOrder,
    DateTime? CreatedOn = null,
    DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn);

public record AttachmentTypeDto(Guid Id, string Name, bool Default, DateTime? CreatedOn = null, DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn);

public record AssetDto(
    Guid Id,
    string Name,
    Guid? CategoryId,
    CategoryDto? Category,
    DateTime? PurchaseDate,
    string? PurchasedFrom,
    Guid? WarrantyTypeId,
    WarrantyTypeDto? WarrantyType,
    DateTime? WarrantyExpiration,
    DateTime? CreatedOn = null,
    DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn);

public record AttachmentDto(
    Guid Id,
    Guid? ParentId,
    string ParentType,
    Guid? AttachmentTypeId,
    AttachmentTypeDto? AttachmentType,
    string OriginFileName,
    string ContentType,
    string Extension,
    float FileSize,
    string Thumb_ContentType,
    string Thumb_Extension,
    float Thumb_FileSize,
    DateTime? CreatedOn = null,
    DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn);

public record NoteDto(
    Guid Id,
    Guid? ParentId,
    string ParentType,
    string Title,
    string Content,
    DateTime? CreatedOn = null,
    DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn);

public record AttributeItemDto(
    Guid Id,
    Guid? ParentId,
    string ParentType,
    string? Key,
    string? Value,
    DateTime? CreatedOn = null,
    DateTime? LastModifiedOn = null)
    : AuditableDto(Id, CreatedOn, LastModifiedOn);

// ── Write models (API requests) ─────────────────────────────────────────────

public record CreateAssetRequest(
    string Name,
    Guid? CategoryId,
    DateTime? PurchaseDate,
    string? PurchasedFrom,
    Guid? WarrantyTypeId,
    DateTime? WarrantyExpiration);

public record UpdateAssetRequest(
    Guid Id,
    string Name,
    Guid? CategoryId,
    DateTime? PurchaseDate,
    string? PurchasedFrom,
    Guid? WarrantyTypeId,
    DateTime? WarrantyExpiration) : IUpdateRequest;

public record CreateCategoryRequest(string Name, string? Icon);

public record UpdateCategoryRequest(Guid Id, string Name, string? Icon) : IUpdateRequest;

public record CreateWarrantyTypeRequest(string Name, int? Days, int? Months, int? Years, bool Default);

public record UpdateWarrantyTypeRequest(Guid Id, string Name, int? Days, int? Months, int? Years, bool Default) : IUpdateRequest;

public record CreateAttachmentTypeRequest(string Name, bool Default);

public record UpdateAttachmentTypeRequest(Guid Id, string Name, bool Default) : IUpdateRequest;

public record CreateAttachmentRequest(
    Guid? ParentId,
    string ParentType,
    Guid? AttachmentTypeId,
    string OriginFileName,
    string ContentType,
    string Extension,
    float FileSize);

public record UpdateAttachmentRequest(
    Guid Id,
    Guid? ParentId,
    string ParentType,
    Guid? AttachmentTypeId,
    string OriginFileName,
    string ContentType,
    string Extension,
    float FileSize) : IUpdateRequest;

public record CreateNoteRequest(Guid? ParentId, string ParentType, string Title, string Content);

public record UpdateNoteRequest(Guid Id, string Title, string Content) : IUpdateRequest;

public record CreateAttributeItemRequest(Guid? ParentId, string ParentType, string? Key, string? Value);

public record UpdateAttributeItemRequest(Guid Id, string? Key, string? Value) : IUpdateRequest;

public record AttachmentUploadRequest(
    string? ParentId,
    string? ParentType,
    string? OriginFileName,
    string? ContentType,
    string? Extension,
    Guid? AttachmentTypeId);
