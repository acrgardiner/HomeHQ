using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels.Setup;

public sealed class WarrantyTypesViewModel : SetupListViewModel<WarrantyTypeDto>
{
    public WarrantyTypesViewModel(ApiClient apiClient, CacheService<WarrantyTypeDto> cache)
        : base(apiClient, cache)
    {
        PageTitle = "Warranty Types";
        SearchPlaceholder = "Search warranty types...";
        EmptyTitle = "No warranty types found";
        EmptyMessage = "Add your first warranty type to get started";
        AddButtonText = "Add Warranty Type";
    }

    protected override string Endpoint => "api/warrantytypes";
    protected override string EditKind => SetupEditViewModel.KindWarrantyType;

    protected override SetupListItem MapItem(WarrantyTypeDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Subtitle = FormatDuration(dto.Years, dto.Months, dto.Days)
    };

    internal static string FormatDuration(int? years, int? months, int? days)
        => $"{years ?? 0} Years {months ?? 0} Months {days ?? 0} Days";
}
