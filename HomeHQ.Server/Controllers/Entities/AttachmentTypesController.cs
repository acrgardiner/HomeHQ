using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AttachmentTypesController : EntitiesController<AttachmentType, AttachmentTypeDto, CreateAttachmentTypeRequest, UpdateAttachmentTypeRequest>
{
    public AttachmentTypesController(IEntityService<AttachmentType> entityService)
        : base(entityService, EntityMappings.AttachmentType)
    {
    }
}
