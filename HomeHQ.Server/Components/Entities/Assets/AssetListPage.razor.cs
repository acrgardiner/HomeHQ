using HomeHQ.Components.Entities.Common;
using HomeHQ.Entities;
using HomeHQ.Server.Components.Entities.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq.Expressions;

namespace HomeHQ.Server.Components.Entities.Assets;

public partial class AssetListPage
{
    [Parameter]
    [SupplyParameterFromQuery(Name = "state")]
    public string? State { get; set; }

    private EntityList<HomeHQ.Entities.Asset>? _entityList;

    private List<EntityAction<HomeHQ.Entities.Asset>>? _AssetActions = null;

    private Func<HomeHQ.Entities.Asset, Task> RowClicked => async (asset) => await OpenDetails(asset);

    // Stats
    private int _totalAssets = 0;
    private int _warrantiesExpiring = 0;
    private int _categoriesCount = 0;

    List<Expression<Func<Asset, object>>>? Includes => new()
    {
        x => x.Category!,
        x => x.WarrantyType!
    };

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Decode state from base64 JSON if present
        string? searchString = null;
        int? page = null;
        int? pageSize = null;

        if (!string.IsNullOrWhiteSpace(State))
        {
            try
            {
                var decodedState = NavigationStateHelper.Decode<NavigationParameters>(State);
                searchString = decodedState?.Search;
                page = decodedState?.Page;
                pageSize = decodedState?.PageSize;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to decode navigation state");
            }
        }

        // Apply query parameters to restore navigation state
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            _entityList?.SetSearchString(searchString);
        }
        else if (_entityList?.SearchString != null)
        {
            // Clear search if no query parameter present
            _entityList?.SetSearchString(string.Empty);
        }

        if (page.HasValue)
        {
            _entityList?.SetPage(page.Value);
        }
        else
        {
            // Reset to first page if no query parameter present
            _entityList?.SetPage(0);
        }

        if (pageSize.HasValue)
        {
            _entityList?.SetRowsPerPage(pageSize.Value);
        }

        var loaddataTask = _entityList?.ReloadData();
        var statisticsTask = LoadStatistics();

        await Task.WhenAll(loaddataTask ?? Task.CompletedTask, statisticsTask);
    }

    private async Task LoadStatistics()
    {
        try
        {
            var stats = await DashboardStats.GetAsync(expiringWithinDays: 30);
            _totalAssets = stats.TotalAssets;
            _warrantiesExpiring = stats.WarrantiesExpiringSoon;
            _categoriesCount = stats.DistinctCategoriesWithAssets;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load asset statistics");
        }
    }

    private Task ShowGalleryAsync(Attachment attachment)
    {
        var param = new DialogParameters<ViewImage>()
        {
            { x => x.Attachment, attachment }
        };
        return DialogService.ShowAsync<ViewImage>("View Attachment", param);
    }

    private void OpenCreate()
    {
        NavigationManager.NavigateTo("/assets/create");
    }

    private async Task OpenDetails(Asset asset)
    {
        var encodedState = EncodeNavigationState();
        var queryString = !string.IsNullOrWhiteSpace(encodedState) ? $"?returnState={Uri.EscapeDataString(encodedState)}" : "";
        NavigationManager.NavigateTo($"/assets/{asset.Id}{queryString}");
    }

    private async Task OpenEdit(Asset asset)
    {
        var encodedState = EncodeNavigationState();
        var queryString = !string.IsNullOrWhiteSpace(encodedState) ? $"?returnState={Uri.EscapeDataString(encodedState)}" : "";
        NavigationManager.NavigateTo($"/assets/{asset.Id}/edit{queryString}");
    }

    // Helper methods for encoding/decoding navigation state
    private string EncodeNavigationState()
    {
        var state = NavigationStateHelper.CreateListState(
            destination: "/assets",
            page: _entityList?.CurrentPage,
            search: _entityList?.SearchString,
            pageSize: _entityList?.RowsPerPage
        );

        return state != null ? NavigationStateHelper.Encode(state) : string.Empty;
    }
}
