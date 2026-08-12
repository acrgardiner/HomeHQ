using System;
using System.Linq.Expressions;

namespace HomeHQ.Components.Entities.Common
{
    public class DataColumn<T>
    {
        public string Title { get; set; } = string.Empty;

        // Primary property expression for the column (used for sorting by default).
        public Expression<Func<T, object>>? Property { get; set; }

        // Optional override for the sort key. If null and Property is null, sorting is disabled for the column.
        public Expression<Func<T, object>>? SortBy { get; set; }

        // Optional renderer used to render the cell. Can return RenderFragment for custom markup, or simple values.
        public Func<T, object?>? Renderer { get; set; }

        public DataColumn() { }

        public DataColumn(System.Linq.Expressions.Expression<Func<T, object>> property, string title, System.Linq.Expressions.Expression<Func<T, object>>? sortBy = null, Func<T, object?>? renderer = null)
        {
            Property = property;
            Title = title;
            SortBy = sortBy ?? property;
            Renderer = renderer ?? (item => property.Compile()(item));
        }
        public DataColumn(Func<T, object?> renderer, string title, System.Linq.Expressions.Expression<Func<T, object>>? sortBy = null)
        {
            Renderer = renderer;
            Title = title;
            SortBy = sortBy;
        }
    }
}
