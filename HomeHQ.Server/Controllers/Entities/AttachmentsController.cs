using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.FileStorage;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AttachmentsController : PolymorphicEntitiesController<Attachment, AttachmentDto, CreateAttachmentRequest, UpdateAttachmentRequest>
{
    private readonly IWebHostEnvironment _env;
    private readonly IFileStorageService _fileStorageService;

    public AttachmentsController(
        IWebHostEnvironment env,
        IEntityService<Attachment> attachmentService,
        IFileStorageService fileStorageService) : base(attachmentService, EntityMappings.Attachment)
    {
        _env = env;
        _fileStorageService = fileStorageService;
    }

    [HttpGet("{attachmentId}/data")]
    [Authorize]
    public async Task<IActionResult> GetFile(string attachmentId)
    {
        var attachment = await EntityService.GetByIdAsync(Guid.Parse(attachmentId));

        if (attachment == null)
        {
            return NotFound();
        }

        var uploadsPath = Path.Combine(_env.ContentRootPath, "appdata", "attachments", attachment.ParentType);
        var filePath = Path.Combine(uploadsPath, attachment.LocalFileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var contentType = attachment?.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }

    [HttpGet("{attachmentId}/previewdata")]
    [Authorize]
    public async Task<IActionResult> GetFilePreview(string attachmentId)
    {
        var attachment = await EntityService.GetByIdAsync(Guid.Parse(attachmentId));

        if (attachment == null)
        {
            return NotFound();
        }

        var uploadsPath = Path.Combine(_env.ContentRootPath, "appdata", "thumbs", attachment.ParentType);
        var filePath = Path.Combine(uploadsPath, attachment.Thumb_LocalFileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var contentType = attachment?.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }

       /// <summary>
    /// Uploads an attachment file with metadata (multipart form data).
    /// Used by mobile clients to upload images shared via Android share intent.
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(15 * 1024 * 1024)] // 15 MB limit
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> Upload([FromForm] IFormFile file, [FromForm] AttachmentUploadRequest metadata)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<AttachmentDto>.Fail("No file provided"));
        }

        if (!Guid.TryParse(metadata.ParentId, out var parentId))
        {
            return BadRequest(ApiResponse<AttachmentDto>.Fail("Invalid parent ID"));
        }

        try
        {
            // Read file bytes
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Create attachment entity
            var attachment = new Attachment
            {
                ParentId = parentId,
                ParentType = metadata.ParentType ?? nameof(Asset),
                OriginFileName = metadata.OriginFileName ?? file.FileName,
                ContentType = metadata.ContentType ?? file.ContentType,
                Extension = metadata.Extension ?? Path.GetExtension(file.FileName),
                FileSize = fileBytes.Length,
                AttachmentTypeId = metadata.AttachmentTypeId ?? Guid.Empty
            };

            // Upload file and generate thumbnails
            await _fileStorageService.UploadAsync<Asset>(fileBytes, attachment);

            // Save attachment entity
            var savedAttachment = await EntityService.AddAsync(attachment);

            return Ok(ApiResponse<AttachmentDto>.Ok(EntityMappings.ToDto(savedAttachment), "Attachment uploaded successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AttachmentDto>.Fail($"Upload failed: {ex.Message}"));
        }
    }
}
