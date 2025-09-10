using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectaardvarkx2.Entities;
using projectaardvarkx2.Services;

namespace projectaardvarkx2.Controllers;

[ApiController]
[Route("api/uploads")]
public class AttachmentFileController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IEntityService<Attachment> _attachmentService;
    public AttachmentFileController(IWebHostEnvironment env, IEntityService<Entities.Attachment> attachmentService)
    {
        _env = env;
        _attachmentService = attachmentService;
    }

    [HttpGet("{attachmentId}")]
    [Authorize]
    public async Task<IActionResult> GetFile(string attachmentId)
    {
        var attachment = await _attachmentService.GetByIdAsync(Guid.Parse(attachmentId));

        if (attachment == null)
            return NotFound();

        var uploadsPath = Path.Combine(_env.ContentRootPath, "appdata", "attachments", attachment.ParentType);
        var filePath = Path.Combine(uploadsPath, attachment.LocalFileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = attachment?.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }

    [HttpGet("{attachmentId}/preview")]
    [Authorize]
    public async Task<IActionResult> GetFilePreview(string attachmentId)
    {
        var attachment = await _attachmentService.GetByIdAsync(Guid.Parse(attachmentId));

        if (attachment == null)
            return NotFound();

        var uploadsPath = Path.Combine(_env.ContentRootPath, "appdata", "thumbs", attachment.ParentType);
        var filePath = Path.Combine(uploadsPath, Path.GetFileNameWithoutExtension(attachment.LocalFileName) + "_thumb.jpg");

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = attachment?.ContentType ?? "application/octet-stream";
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }
}
