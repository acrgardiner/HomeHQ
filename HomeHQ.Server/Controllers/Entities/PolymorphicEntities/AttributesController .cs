using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AttributesController : PolymorphicEntitiesController<Entities.Attribute, AttributeItemDto, CreateAttributeItemRequest, UpdateAttributeItemRequest>
{
    public AttributesController(IEntityService<Entities.Attribute> entityService)
        : base(entityService, EntityMappings.Attribute)
    {
    }
}
