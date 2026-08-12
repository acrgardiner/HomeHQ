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
public class CategoriesController : EntitiesController<Category, CategoryDto, CreateCategoryRequest, UpdateCategoryRequest>
{
    public CategoriesController(IEntityService<Category> entityService)
        : base(entityService, EntityMappings.Category)
    {
    }
}
