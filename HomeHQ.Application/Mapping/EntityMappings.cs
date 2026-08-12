using HomeHQ.DTOs;
using HomeHQ.Entities;

namespace HomeHQ.Application.Mapping;

public static class EntityMappings
{
    public static readonly EntityApiMapping<Asset, AssetDto, CreateAssetRequest, UpdateAssetRequest> Asset =
        new(ToDto, FromCreate, ApplyUpdate);

    public static readonly EntityApiMapping<Category, CategoryDto, CreateCategoryRequest, UpdateCategoryRequest> Category =
        new(ToDto, FromCreate, ApplyUpdate);

    public static readonly EntityApiMapping<WarrantyType, WarrantyTypeDto, CreateWarrantyTypeRequest, UpdateWarrantyTypeRequest> WarrantyType =
        new(ToDto, FromCreate, ApplyUpdate);

    public static readonly EntityApiMapping<AttachmentType, AttachmentTypeDto, CreateAttachmentTypeRequest, UpdateAttachmentTypeRequest> AttachmentType =
        new(ToDto, FromCreate, ApplyUpdate);

    public static readonly EntityApiMapping<Attachment, AttachmentDto, CreateAttachmentRequest, UpdateAttachmentRequest> Attachment =
        new(ToDto, FromCreate, ApplyUpdate);

    public static readonly EntityApiMapping<Note, NoteDto, CreateNoteRequest, UpdateNoteRequest> Note =
        new(ToDto, FromCreate, ApplyUpdate);

    public static readonly EntityApiMapping<Entities.Attribute, AttributeItemDto, CreateAttributeItemRequest, UpdateAttributeItemRequest> Attribute =
        new(ToDto, FromCreate, ApplyUpdate);

    // ── Asset ─────────────────────────────────────────────────────────────────

    public static AssetDto ToDto(Asset entity) => new(
        entity.Id,
        entity.Name,
        entity.CategoryId,
        entity.Category is null ? null : ToDto(entity.Category),
        entity.PurchaseDate,
        entity.PurchasedFrom,
        entity.WarrantyTypeId,
        entity.WarrantyType is null ? null : ToDto(entity.WarrantyType),
        entity.WarrantyExpiration,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static Asset ToEntity(AssetDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        CategoryId = dto.CategoryId,
        Category = dto.Category is null ? null : ToEntity(dto.Category),
        PurchaseDate = dto.PurchaseDate,
        PurchasedFrom = dto.PurchasedFrom,
        WarrantyTypeId = dto.WarrantyTypeId,
        WarrantyType = dto.WarrantyType is null ? null : ToEntity(dto.WarrantyType),
        WarrantyExpiration = dto.WarrantyExpiration,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    public static CreateAssetRequest ToCreateRequest(Asset entity) => new(
        entity.Name,
        entity.CategoryId,
        entity.PurchaseDate,
        entity.PurchasedFrom,
        entity.WarrantyTypeId,
        entity.WarrantyExpiration);

    public static UpdateAssetRequest ToUpdateRequest(Asset entity) => new(
        entity.Id,
        entity.Name,
        entity.CategoryId,
        entity.PurchaseDate,
        entity.PurchasedFrom,
        entity.WarrantyTypeId,
        entity.WarrantyExpiration);

    private static Asset FromCreate(CreateAssetRequest request) => new()
    {
        Name = request.Name,
        CategoryId = request.CategoryId,
        PurchaseDate = request.PurchaseDate,
        PurchasedFrom = request.PurchasedFrom,
        WarrantyTypeId = request.WarrantyTypeId,
        WarrantyExpiration = request.WarrantyExpiration
    };

    private static void ApplyUpdate(Asset entity, UpdateAssetRequest request)
    {
        entity.Name = request.Name;
        entity.CategoryId = request.CategoryId;
        entity.PurchaseDate = request.PurchaseDate;
        entity.PurchasedFrom = request.PurchasedFrom;
        entity.WarrantyTypeId = request.WarrantyTypeId;
        entity.WarrantyExpiration = request.WarrantyExpiration;
    }

    // ── Category ────────────────────────────────────────────────────────────────

    public static CategoryDto ToDto(Category entity) => new(
        entity.Id,
        entity.Name,
        entity.Icon,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static Category ToEntity(CategoryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Icon = dto.Icon,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    public static CreateCategoryRequest ToCreateRequest(Category entity) => new(
        entity.Name,
        entity.Icon);

    public static UpdateCategoryRequest ToUpdateRequest(Category entity) => new(
        entity.Id,
        entity.Name,
        entity.Icon);

    private static Category FromCreate(CreateCategoryRequest request) => new()
    {
        Name = request.Name,
        Icon = request.Icon
    };

    private static void ApplyUpdate(Category entity, UpdateCategoryRequest request)
    {
        entity.Name = request.Name;
        entity.Icon = request.Icon;
    }

    // ── WarrantyType ────────────────────────────────────────────────────────────

    public static WarrantyTypeDto ToDto(WarrantyType entity) => new(
        entity.Id,
        entity.Name,
        entity.Days,
        entity.Months,
        entity.Years,
        entity.Default,
        entity.SortOrder,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static WarrantyType ToEntity(WarrantyTypeDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Days = dto.Days,
        Months = dto.Months,
        Years = dto.Years,
        Default = dto.Default,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    public static CreateWarrantyTypeRequest ToCreateRequest(WarrantyType entity) => new(
        entity.Name,
        entity.Days,
        entity.Months,
        entity.Years,
        entity.Default);

    public static UpdateWarrantyTypeRequest ToUpdateRequest(WarrantyType entity) => new(
        entity.Id,
        entity.Name,
        entity.Days,
        entity.Months,
        entity.Years,
        entity.Default);

    private static WarrantyType FromCreate(CreateWarrantyTypeRequest request) => new()
    {
        Name = request.Name,
        Days = request.Days,
        Months = request.Months,
        Years = request.Years,
        Default = request.Default
    };

    private static void ApplyUpdate(WarrantyType entity, UpdateWarrantyTypeRequest request)
    {
        entity.Name = request.Name;
        entity.Days = request.Days;
        entity.Months = request.Months;
        entity.Years = request.Years;
        entity.Default = request.Default;
    }

    // ── AttachmentType ──────────────────────────────────────────────────────────

    public static AttachmentTypeDto ToDto(AttachmentType entity) => new(
        entity.Id,
        entity.Name,
        entity.Default,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static AttachmentType ToEntity(AttachmentTypeDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Default = dto.Default,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    public static CreateAttachmentTypeRequest ToCreateRequest(AttachmentType entity) => new(
        entity.Name,
        entity.Default);

    public static UpdateAttachmentTypeRequest ToUpdateRequest(AttachmentType entity) => new(
        entity.Id,
        entity.Name,
        entity.Default);

    private static AttachmentType FromCreate(CreateAttachmentTypeRequest request) => new()
    {
        Name = request.Name,
        Default = request.Default
    };

    private static void ApplyUpdate(AttachmentType entity, UpdateAttachmentTypeRequest request)
    {
        entity.Name = request.Name;
        entity.Default = request.Default;
    }

    // ── Attachment ──────────────────────────────────────────────────────────────

    public static AttachmentDto ToDto(Attachment entity) => new(
        entity.Id,
        entity.ParentId,
        entity.ParentType,
        entity.AttachmentTypeId,
        entity.AttachmentType is null ? null : ToDto(entity.AttachmentType),
        entity.OriginFileName,
        entity.ContentType,
        entity.Extension,
        entity.FileSize,
        entity.Thumb_ContentType,
        entity.Thumb_Extension,
        entity.Thumb_FileSize,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static Attachment ToEntity(AttachmentDto dto) => new()
    {
        Id = dto.Id,
        ParentId = dto.ParentId,
        ParentType = dto.ParentType,
        AttachmentTypeId = dto.AttachmentTypeId,
        AttachmentType = dto.AttachmentType is null ? null : ToEntity(dto.AttachmentType),
        OriginFileName = dto.OriginFileName,
        ContentType = dto.ContentType,
        Extension = dto.Extension,
        FileSize = dto.FileSize,
        Thumb_ContentType = dto.Thumb_ContentType,
        Thumb_Extension = dto.Thumb_Extension,
        Thumb_FileSize = dto.Thumb_FileSize,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    private static Attachment FromCreate(CreateAttachmentRequest request) => new()
    {
        ParentId = request.ParentId,
        ParentType = request.ParentType,
        AttachmentTypeId = request.AttachmentTypeId,
        OriginFileName = request.OriginFileName,
        ContentType = request.ContentType,
        Extension = request.Extension,
        FileSize = request.FileSize
    };

    private static void ApplyUpdate(Attachment entity, UpdateAttachmentRequest request)
    {
        entity.ParentId = request.ParentId;
        entity.ParentType = request.ParentType;
        entity.AttachmentTypeId = request.AttachmentTypeId;
        entity.OriginFileName = request.OriginFileName;
        entity.ContentType = request.ContentType;
        entity.Extension = request.Extension;
        entity.FileSize = request.FileSize;
    }

    // ── Note ────────────────────────────────────────────────────────────────────

    public static NoteDto ToDto(Note entity) => new(
        entity.Id,
        entity.ParentId,
        entity.ParentType,
        entity.Title,
        entity.Content,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static Note ToEntity(NoteDto dto) => new()
    {
        Id = dto.Id,
        ParentId = dto.ParentId,
        ParentType = dto.ParentType,
        Title = dto.Title,
        Content = dto.Content,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    public static CreateNoteRequest ToCreateRequest(Note entity) => new(
        entity.ParentId,
        entity.ParentType,
        entity.Title,
        entity.Content);

    public static UpdateNoteRequest ToUpdateRequest(Note entity) => new(
        entity.Id,
        entity.Title,
        entity.Content);

    private static Note FromCreate(CreateNoteRequest request) => new()
    {
        ParentId = request.ParentId,
        ParentType = request.ParentType,
        Title = request.Title,
        Content = request.Content
    };

    private static void ApplyUpdate(Note entity, UpdateNoteRequest request)
    {
        entity.Title = request.Title;
        entity.Content = request.Content;
    }

    // ── Attribute ───────────────────────────────────────────────────────────────

    public static AttributeItemDto ToDto(Entities.Attribute entity) => new(
        entity.Id,
        entity.ParentId,
        entity.ParentType,
        entity.Key,
        entity.Value,
        entity.CreatedOn,
        entity.LastModifiedOn);

    public static Entities.Attribute ToEntity(AttributeItemDto dto) => new()
    {
        Id = dto.Id,
        ParentId = dto.ParentId,
        ParentType = dto.ParentType,
        Key = dto.Key,
        Value = dto.Value,
        CreatedOn = dto.CreatedOn,
        LastModifiedOn = dto.LastModifiedOn
    };

    public static CreateAttributeItemRequest ToCreateRequest(Entities.Attribute entity) => new(
        entity.ParentId,
        entity.ParentType,
        entity.Key,
        entity.Value);

    public static UpdateAttributeItemRequest ToUpdateRequest(Entities.Attribute entity) => new(
        entity.Id,
        entity.Key,
        entity.Value);

    private static Entities.Attribute FromCreate(CreateAttributeItemRequest request) => new()
    {
        ParentId = request.ParentId,
        ParentType = request.ParentType,
        Key = request.Key,
        Value = request.Value
    };

    private static void ApplyUpdate(Entities.Attribute entity, UpdateAttributeItemRequest request)
    {
        entity.Key = request.Key;
        entity.Value = request.Value;
    }
}
