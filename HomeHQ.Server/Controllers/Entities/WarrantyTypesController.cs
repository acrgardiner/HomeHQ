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
public class WarrantyTypesController : EntitiesController<WarrantyType, WarrantyTypeDto, CreateWarrantyTypeRequest, UpdateWarrantyTypeRequest>
{
    public WarrantyTypesController(IEntityService<WarrantyType> entityService)
        : base(entityService, EntityMappings.WarrantyType)
    {
    }
}
