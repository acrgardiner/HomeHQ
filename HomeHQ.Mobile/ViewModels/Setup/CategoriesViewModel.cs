using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels.Setup;

public sealed class CategoriesViewModel : SetupListViewModel<CategoryDto>
{
    public CategoriesViewModel(ApiClient apiClient, CacheService<CategoryDto> cache)
        : base(apiClient, cache)
    {
        PageTitle = "Categories";
        SearchPlaceholder = "Search categories...";
        EmptyTitle = "No categories found";
        EmptyMessage = "Add your first category to get started";
        AddButtonText = "Add Category";
    }

    protected override string Endpoint => "api/categories";
    protected override string EditKind => SetupEditViewModel.KindCategory;

    protected override SetupListItem MapItem(CategoryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        LeadingText = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon
    };
}
