using HomeHQ.Contracts;
using HomeHQ.DTOs;
using HomeHQ.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

public abstract class EntitiesController<TEntity, TDto, TCreate, TUpdate> : ControllerBase
    where TEntity : AuditableEntity, IEntity<Guid>
    where TDto : IEntityDto
    where TUpdate : IUpdateRequest
{
    protected readonly Application.Mapping.EntityApiMapping<TEntity, TDto, TCreate, TUpdate> Mapping;

    protected IEntityService<TEntity> EntityService { get; }

    protected EntitiesController(
        IEntityService<TEntity> entityService,
        Application.Mapping.EntityApiMapping<TEntity, TDto, TCreate, TUpdate> mapping)
    {
        EntityService = entityService;
        Mapping = mapping;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TDto>>>> GetAll()
    {
        var entities = await EntityService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<TDto>>.Ok(Mapping.ToDtos(entities)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TDto>>> GetById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Ok(ApiResponse<TDto>.Fail("Invalid GUID format for 'id'."));
        }

        var entity = await EntityService.GetByIdAsync(id);

        if (entity == null)
        {
            return Ok(ApiResponse<TDto>.Fail($"{typeof(TEntity).Name} not found"));
        }

        return Ok(ApiResponse<TDto>.Ok(Mapping.ToDto(entity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TDto>>> Create([FromBody] TCreate request)
    {
        var entity = Mapping.FromCreate(request);
        var created = await EntityService.AddAsync(entity);
        return Ok(ApiResponse<TDto>.Ok(Mapping.ToDto(created), $"{typeof(TEntity).Name} created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TDto>>> Update(Guid id, [FromBody] TUpdate request)
    {
        if (id == Guid.Empty)
        {
            return Ok(ApiResponse<TDto>.Fail("Invalid GUID format for 'id'."));
        }

        if (id != request.Id)
        {
            return Ok(ApiResponse<TDto>.Fail("ID mismatch"));
        }

        var existing = await EntityService.GetByIdAsync(id);
        if (existing == null)
        {
            return Ok(ApiResponse<TDto>.Fail($"{typeof(TEntity).Name} not found"));
        }

        Mapping.ApplyUpdate(existing, request);
        var updated = await EntityService.UpdateAsync(existing);
        return Ok(ApiResponse<TDto>.Ok(Mapping.ToDto(updated), $"{typeof(TEntity).Name} updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Ok(ApiResponse.Fail("Invalid GUID format for 'id'."));
        }

        var existing = await EntityService.GetByIdAsync(id);
        if (existing == null)
        {
            return Ok(ApiResponse.Fail($"{typeof(TEntity).Name} not found"));
        }

        await EntityService.DeleteAsync(id);
        return Ok(ApiResponse.Ok($"{typeof(TEntity).Name} deleted successfully"));
    }
}
