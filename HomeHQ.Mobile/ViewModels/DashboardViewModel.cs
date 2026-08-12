using HomeHQ.Mobile.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CacheService<CategoryDto> _categoryCache;

    public ObservableCollection<Asset> RecentAssets { get; } = [];
    public int TotalAssets
    {
        get;
        set => SetProperty(ref field, value);
    }
    public int ExpiringWarranties
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
            OnPropertyChanged(nameof(ShowContent));
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
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool ShowContent => !IsLoading && !HasError;
    public bool HasRecentAssets => RecentAssets.Count > 0;

    public ICommand RefreshCommand { get; }
    public ICommand NavigateToAssetsCommand { get; }
    public ICommand NavigateToAssetCommand { get; }
    public ICommand AssetSelectedCommand { get; }
    public ICommand AddAssetCommand { get; }

    public DashboardViewModel(ApiClient apiClient, CacheService<CategoryDto> categoryCache)
    {
        _apiClient = apiClient;
        _categoryCache = categoryCache;

        RefreshCommand = new Command(async () => await LoadDashboardAsync(forceRefresh: true));
        NavigateToAssetsCommand = new Command(async () => await NavigateToAssetsAsync());
        AssetSelectedCommand = new Command<Asset>(async (asset) => await OnAssetSelected(asset));
        AddAssetCommand = new Command(async () => await AddAssetAsync());
    }

    public async Task LoadDashboardAsync(bool forceRefresh = false)
    {
        if (IsLoading && !forceRefresh)
        {
            return;
        }

        try
        {
            IsLoading = !forceRefresh;
            IsRefreshing = forceRefresh;
            ErrorMessage = null;

            // Ensure categories are loaded (uses shared cache)
            await _categoryCache.EnsureLoadedAsync(forceRefresh);

            var statsResponse = await _apiClient.GetAsync("api/assets/dashboard-stats?expiringWithinDays=90");
            var assetsResponse = await _apiClient.GetAsync("api/assets");

            if (statsResponse.IsSuccessStatusCode)
            {
                var stats = await statsResponse.Content.ReadFromJsonAsync<ApiResponse<AssetDashboardStatsDto>>();
                if (stats?.Data != null)
                {
                    TotalAssets = stats.Data.TotalAssets;
                    ExpiringWarranties = stats.Data.WarrantiesExpiringSoon;
                }
            }

            if (assetsResponse.IsSuccessStatusCode)
            {
                var apiResponse = await assetsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AssetDto>>>();

                if (apiResponse?.Data != null)
                {
                    RecentAssets.Clear();
                    var recentItems = apiResponse.Data
                        .Select(EntityMappings.ToEntity)
                        .OrderByDescending(a => a.CreatedOn)
                        .Take(5);

                    _categoryCache.PopulateAll(
                        recentItems,
                        a => a.CategoryId,
                        (a, c) => a.Category = c is null ? null : EntityMappings.ToEntity(c));

                    foreach (var asset in recentItems)
                    {
                        RecentAssets.Add(asset);
                    }
                }

                OnPropertyChanged(nameof(HasRecentAssets));
            }
            else if (assetsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
                     || statsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }
            else if (!assetsResponse.IsSuccessStatusCode)
            {
                ErrorMessage = $"Failed to load assets: {assetsResponse.StatusCode}";
            }
            else if (!statsResponse.IsSuccessStatusCode)
            {
                ErrorMessage = $"Failed to load dashboard stats: {statsResponse.StatusCode}";
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
        }
    }

    private async Task NavigateToAssetsAsync()
    {
        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("//Assets"),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task OnAssetSelected(Asset? asset)
    {
        if (asset == null)
        {
            return;
        }

        var param = new Dictionary<string, object>
        {
            { "assetId", asset.Id.ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("AssetDetail", param),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task AddAssetAsync()
    {
        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("Asset/Create"),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }
}
