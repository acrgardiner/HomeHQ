using HomeHQ.Contracts;
using HomeHQ.DTOs;
using HomeHQ.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

public class EntitiesController<TEntity> : ControllerBase where TEntity : IAuditableEntity, IEntity<Guid>
{
    internal readonly IEntityService<TEntity> _entityService;

    public EntitiesController(IEntityService<TEntity> entityService)
    {
        _entityService = entityService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TEntity>>>> GetAll()
    {
        var entities = await _entityService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<TEntity>>.Ok(entities));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TEntity>>> GetById(Guid id)
    {
        if (id == Guid.Empty)
            return Ok(ApiResponse<TEntity>.Fail("Invalid GUID format for 'id'."));

        var entity = await _entityService.GetByIdAsync(id);

        if (entity == null)
        {
            return Ok(ApiResponse<TEntity>.Fail($"{typeof(TEntity)} not found"));
        }

        return Ok(ApiResponse<TEntity>.Ok(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TEntity>>> Create([FromBody] TEntity entity)
    {
        var created = await _entityService.AddAsync(entity);
        return Ok(ApiResponse<TEntity>.Ok(created, $"{typeof(TEntity)} created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TEntity>>> Update(Guid id, [FromBody] TEntity entity)
    {
        if (id == Guid.Empty)
        {
            return Ok(ApiResponse<TEntity>.Fail("Invalid GUID format for 'id'."));
        }

        if (id != entity.Id)
        {
            return Ok(ApiResponse<TEntity>.Fail("ID mismatch"));
        }

        var existing = await _entityService.GetByIdAsync(id);
        if (existing == null)
        {
            return Ok(ApiResponse<TEntity>.Fail($"{typeof(TEntity)} not found"));
        }

        var updated = await _entityService.UpdateAsync(entity);
        return Ok(ApiResponse<TEntity>.Ok(updated, $"{typeof(TEntity)} updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Ok(ApiResponse.Fail("Invalid GUID format for 'id'."));
        }

        var existing = await _entityService.GetByIdAsync(id);
        if (existing == null)
        {
            return Ok(ApiResponse.Fail($"{typeof(TEntity)} not found"));
        }

        await _entityService.DeleteAsync(id);
        return Ok(ApiResponse.Ok($"{typeof(TEntity)} deleted successfully"));
    }
}
