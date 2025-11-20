namespace HomeHQ.Components.Entities.Common
{
    public class EntityAction<TEntity>
    {
        public string Icon { get; set; } = default!;
        public string Tooltip { get; set; } = default!;
        public Func<TEntity, Task> Action { get; set; } = default!;
    }
}
