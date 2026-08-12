using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public class AssetsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CacheService<CategoryDto> _categoryCache;
    private readonly CacheService<WarrantyTypeDto> _warrantyTypeCache;
    private readonly SettingsService _settingsService;

    public ObservableCollection<AssetListItem> Assets { get; } = [];

    private List<AssetListItem> _allAssets = [];

    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = string.Empty;

    public int TotalAssets
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int WarrantiesExpiring
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsLoading
    {
        get;
        set
        {
            SetProperty(ref field, value);
            UpdateVisibility();
        }
    }

    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? ErrorMessage
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(HasError));
            UpdateVisibility();
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool ShowEmptyState
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowList
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowStats
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AssetSelectedCommand { get; }
    public ICommand AddAssetCommand { get; }

    public AssetsViewModel(
        ApiClient apiClient,
        CacheService<CategoryDto> categoryCache,
        CacheService<WarrantyTypeDto> warrantyTypeCache,
        SettingsService settingsService)
    {
        _apiClient = apiClient;
        _categoryCache = categoryCache;
        _warrantyTypeCache = warrantyTypeCache;
        _settingsService = settingsService;

        RefreshCommand = new Command(async () => await LoadAssetsAsync(forceRefresh: true));
        SearchCommand = new Command(ApplyFilter);
        AssetSelectedCommand = new Command<AssetListItem>(async item => await OnAssetSelected(item));
        AddAssetCommand = new Command(async () => await AddAssetAsync());
    }

    public async Task LoadAssetsAsync(bool forceRefresh = false)
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = !forceRefresh;
            IsRefreshing = forceRefresh;
            ErrorMessage = null;

            await Task.WhenAll(
                _categoryCache.EnsureLoadedAsync(forceRefresh),
                _warrantyTypeCache.EnsureLoadedAsync(forceRefresh));

            var assetsTask = _apiClient.GetAsync("api/assets");
            // Match Blazor AssetListPage: warranties expiring within 30 days
            var statsTask = _apiClient.GetAsync("api/assets/dashboard-stats?expiringWithinDays=30");
            await Task.WhenAll(assetsTask, statsTask);

            var response = await assetsTask;
            var statsResponse = await statsTask;

            if (statsResponse.IsSuccessStatusCode)
            {
                var stats = await statsResponse.Content.ReadFromJsonAsync<ApiResponse<AssetDashboardStatsDto>>();
                if (stats?.Data != null)
                {
                    TotalAssets = stats.Data.TotalAssets;
                    WarrantiesExpiring = stats.Data.WarrantiesExpiringSoon;
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<AssetDto>>>();

                Assets.Clear();
                _allAssets.Clear();

                if (apiResponse?.Data?.Count > 0)
                {
                    var assets = apiResponse.Data.Select(EntityMappings.ToEntity).ToList();
                    _categoryCache.PopulateAll(
                        assets,
                        a => a.CategoryId,
                        (a, c) => a.Category = c is null ? null : EntityMappings.ToEntity(c));
                    _warrantyTypeCache.PopulateAll(
                        assets,
                        a => a.WarrantyTypeId,
                        (a, w) => a.WarrantyType = w is null ? null : EntityMappings.ToEntity(w));

                    _allAssets = assets
                        .Select(a => new AssetListItem { Asset = a })
                        .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    ApplyFilter();
                }
                else
                {
                    TotalAssets = 0;
                    ApplyFilter();
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }
            else
            {
                ErrorMessage = $"Failed to load assets: {response.StatusCode}";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Connection error: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            UpdateVisibility();
        }
    }

    private void ApplyFilter()
    {
        var list = _allAssets;

        var results = string.IsNullOrWhiteSpace(SearchText)
            ? list
            : list.Where(MatchesSearch).ToList();

        Assets.Clear();
        foreach (var item in results)
        {
            Assets.Add(item);
        }

        UpdateVisibility();
    }

    private bool MatchesSearch(AssetListItem item)
    {
        var term = SearchText;
        return (!string.IsNullOrEmpty(item.Name) && item.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(item.PurchasedFrom) && item.PurchasedFrom.Contains(term, StringComparison.OrdinalIgnoreCase))
            || item.CategoryTitle.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (item.HasPurchaseDate && item.PurchaseDateFormatted.Contains(term, StringComparison.OrdinalIgnoreCase))
            || item.WarrantyStatusText.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (item.HasWarranty && item.WarrantyDateFormatted.Contains(term, StringComparison.OrdinalIgnoreCase))
            || (item.Asset.WarrantyType?.Name is { Length: > 0 } warrantyName
                && warrantyName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private async Task OnAssetSelected(AssetListItem? item)
    {
        if (item == null)
        {
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "assetId", item.Id.ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("AssetDetail", parameters),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task AddAssetAsync()
    {
        try
        {
            var param = new Dictionary<string, object>
            {
                { "assetId", Guid.Empty.ToString() }
            };
            await SafeExecuteAsync(
                () => Shell.Current.GoToAsync("AssetEdit", param),
                onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            try
            {
                var createUrl = $"{_settingsService.ApiBaseUrl}/assets/create";
                await Browser.OpenAsync(createUrl, BrowserLaunchMode.External);
            }
            catch (Exception inner)
            {
                ErrorMessage = $"Could not open create page: {ex.Message}; {inner.Message}";
            }
        }
    }

    private void UpdateVisibility()
    {
        ShowEmptyState = !IsLoading && !HasError && Assets.Count == 0;
        ShowList = !IsLoading && !HasError && Assets.Count > 0;
        ShowStats = !IsLoading && !HasError;
    }
}
