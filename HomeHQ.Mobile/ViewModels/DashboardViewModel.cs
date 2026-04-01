using HomeHQ.Mobile.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CategoryService _categoryService;

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

    public DashboardViewModel(ApiClient apiClient, CategoryService categoryService)
    {
        _apiClient = apiClient;
        _categoryService = categoryService;

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
            await _categoryService.EnsureLoadedAsync(forceRefresh);

            // Load assets to get counts and recent items
            var response = await _apiClient.GetAsync("api/assets");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<Asset>>>();

                if (apiResponse?.Data != null)
                {
                    var assets = apiResponse.Data;
                    
                    // Populate categories from shared service
                    _categoryService.PopulateCategories(assets);
                    
                    // Update statistics
                    TotalAssets = assets.Count;
                    
                    // Calculate expiring warranties (within 90 days)
                    var expirationThreshold = DateTime.UtcNow.AddDays(90);
                    ExpiringWarranties = assets.Count(a => 
                        a.WarrantyExpiration.HasValue && 
                        a.WarrantyExpiration.Value >= DateTime.UtcNow &&
                        a.WarrantyExpiration.Value <= expirationThreshold);

                    // Get recent assets (last 5)
                    RecentAssets.Clear();
                    var recentItems = assets
                        .OrderByDescending(a => a.CreatedOn)
                        .Take(5);
                    
                    foreach (var asset in recentItems)
                    {
                        RecentAssets.Add(asset);
                    }
                }
                
                OnPropertyChanged(nameof(HasRecentAssets));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }
            else
            {
                ErrorMessage = $"Failed to load dashboard: {response.StatusCode}";
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

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync($"AssetDetail?assetId={asset.Id}"),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task AddAssetAsync()
    {
        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("Asset/Create"),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }
}
