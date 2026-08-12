namespace HomeHQ.Application.Mapping;

public sealed class EntityApiMapping<TEntity, TDto, TCreate, TUpdate>(
    Func<TEntity, TDto> toDto,
    Func<TCreate, TEntity> fromCreate,
    Action<TEntity, TUpdate> applyUpdate)
{
    public TDto ToDto(TEntity entity) => toDto(entity);

    public IEnumerable<TDto> ToDtos(IEnumerable<TEntity> entities) => entities.Select(ToDto);

    public TEntity FromCreate(TCreate request) => fromCreate(request);

    public void ApplyUpdate(TEntity entity, TUpdate request) => applyUpdate(entity, request);
}
