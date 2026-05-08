using HomeHQ.Services;
using HomeHQ.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AttributesController : PolymorphicEntitiesController<Entities.Attribute>
{
    public AttributesController(IEntityService<Entities.Attribute> entityService) : base(entityService)
    {
    }
}
