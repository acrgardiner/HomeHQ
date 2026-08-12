using HomeHQ.Application.Polymorphism;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Polymorphism;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

/// <summary>
/// API base for polymorphic child entities (attachments, notes, attributes).
/// Provides by-parent queries and optional delete hooks (e.g. attachment file cleanup).
/// Parent entities (Asset) use <see cref="EntitiesController{TEntity,TDto,TCreate,TUpdate}"/>;
/// their cascade delete runs via <see cref="PolymorphicEntityDeletion"/> in <see cref="IEntityService{T}"/>.
/// </summary>
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

    public override async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Ok(ApiResponse.Fail("Invalid GUID format for 'id'."));
        }

        var existing = await EntityService.GetByIdAsync(id);
        if (existing is null)
        {
            return Ok(ApiResponse.Fail($"{typeof(TEntity).Name} not found"));
        }

        await EntityService.DeleteAsync(id);
        return Ok(ApiResponse.Ok($"{typeof(TEntity).Name} deleted successfully"));
    }
}
