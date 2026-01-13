using HomeHQ.Entities;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : EntitiesController<Asset>
{
    public AssetsController(IEntityService<Asset> entityService) : base(entityService)
    {
    }
}
