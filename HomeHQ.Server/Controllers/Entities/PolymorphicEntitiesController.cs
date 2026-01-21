using HomeHQ.Contracts;
using HomeHQ.DTOs;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PolymorphicEntitiesController<TEntity> : EntitiesController<TEntity> where TEntity : IAuditableEntity, IEntity<Guid>, IPolymorphicEntity
{
    public PolymorphicEntitiesController(IEntityService<TEntity> entityService) : base(entityService)
    {
    }

    [HttpGet("by-parent/{parentType}/{parentId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TEntity>>>> GetByParent(string parentType, string parentId)
    {
        if (!Guid.TryParse(parentId, out var parentGuid))
            return BadRequest(ApiResponse<IEnumerable<TEntity>>.Fail("Invalid parentId format. Must be a valid GUID."));

        var entities = await _entityService.GetAsync(n => n.ParentType == parentType && n.ParentId == parentGuid);
        return Ok(ApiResponse<IEnumerable<TEntity>>.Ok(entities));
    }
}
