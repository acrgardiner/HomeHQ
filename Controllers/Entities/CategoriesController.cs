using HomeHQ.Services;
using HomeHQ.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : EntitiesController<Category>
{
    public CategoriesController(IEntityService<Category> entityService) : base(entityService)
    {
    }
}
