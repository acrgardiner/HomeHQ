using HomeHQ.Contracts;
using HomeHQ.DTOs;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public abstract class PolymorphicEntitiesController<TEntity, TDto, TCreate, TUpdate> : EntitiesController<TEntity, TDto, TCreate, TUpdate>
    where TEntity : AuditableEntity, IEntity<Guid>, IPolymorphicEntity
    where TDto : IEntityDto
    where TUpdate : IUpdateRequest
{
    protected PolymorphicEntitiesController(
        IEntityService<TEntity> entityService,
        Application.Mapping.EntityApiMapping<TEntity, TDto, TCreate, TUpdate> mapping)
        : base(entityService, mapping)
    {
    }

    [HttpGet("by-parent/{parentType}/{parentId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TDto>>>> GetByParent(string parentType, string parentId)
    {
        if (!Guid.TryParse(parentId, out var parentGuid))
        {
            return BadRequest(ApiResponse<IEnumerable<TDto>>.Fail("Invalid parentId format. Must be a valid GUID."));
        }

        var entities = await EntityService.GetAsync(n => n.ParentType == parentType && n.ParentId == parentGuid);
        return Ok(ApiResponse<IEnumerable<TDto>>.Ok(Mapping.ToDtos(entities)));
    }
}
