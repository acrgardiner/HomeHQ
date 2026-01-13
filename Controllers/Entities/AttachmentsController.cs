using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HomeHQ.Entities;
using HomeHQ.Services;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController : PolymorphicEntitiesController<Attachment>
{
    private readonly IWebHostEnvironment _env;
    public AttachmentsController(IWebHostEnvironment env, IEntityService<Attachment> attachmentService) : base(attachmentService)
    {
        _env = env;
    }

    [HttpGet("{attachmentId}/data")]
    [Authorize]
    public async Task<IActionResult> GetFile(string attachmentId)
    {
        var attachment = await _entityService.GetByIdAsync(Guid.Parse(attachmentId));

        if (attachment == null)
            return NotFound();

        var uploadsPath = Path.Combine(_env.ContentRootPath, "appdata", "attachments", attachment.ParentType);
        var filePath = Path.Combine(uploadsPath, attachment.LocalFileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = attachment?.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }

    [HttpGet("{attachmentId}/previewdata")]
    [Authorize]
    public async Task<IActionResult> GetFilePreview(string attachmentId)
    {
        var attachment = await _entityService.GetByIdAsync(Guid.Parse(attachmentId));

        if (attachment == null)
            return NotFound();

        var uploadsPath = Path.Combine(_env.ContentRootPath, "appdata", "thumbs", attachment.ParentType);
        var filePath = Path.Combine(uploadsPath, attachment.Thumb_LocalFileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = attachment?.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }
}
