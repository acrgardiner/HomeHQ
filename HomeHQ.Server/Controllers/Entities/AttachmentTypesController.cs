using HomeHQ.Services;
using HomeHQ.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AttachmentTypesController : EntitiesController<AttachmentType>
{
    public AttachmentTypesController(IEntityService<AttachmentType> entityService) : base(entityService)
    {
    }
}
