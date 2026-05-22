using HomeHQ.Mobile.Services;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.ViewModels;

public class AssetsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly CacheService<CategoryDto> _categoryCache;
    private readonly SettingsService _settingsService;

    public ObservableCollection<Asset> Assets { get; } = [];
    // Backing store for all loaded assets used for local filtering
    private List<Asset> _allAssets = new();
    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Apply local filter as the user types
                ApplyFilter();
            }
        }
    } = string.Empty;

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

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AssetSelectedCommand { get; }
    public ICommand AddAssetCommand { get; }

    public AssetsViewModel(ApiClient apiClient, AuthService authService, CacheService<CategoryDto> categoryCache, SettingsService settingsService)
    {
        _apiClient = apiClient;
        _authService = authService;
        _categoryCache = categoryCache;
        _settingsService = settingsService;

        RefreshCommand = new Command(async () => await LoadAssetsAsync(forceRefresh: true));
        SearchCommand = new Command(async () => await SearchAssetsAsync());
        AssetSelectedCommand = new Command<Asset>(async (asset) => await OnAssetSelected(asset));
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

            // Ensure categories are loaded (uses shared cache)
            await _categoryCache.EnsureLoadedAsync(forceRefresh);

            var response = await _apiClient.GetAsync("api/assets");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<AssetDto>>>();

                // Clear previous lists
                Assets.Clear();
                _allAssets.Clear();

                if (apiResponse?.Data?.Count > 0)
                {
                    var assets = apiResponse.Data.Select(EntityMappings.ToEntity).ToList();
                    _categoryCache.PopulateAll(
                        assets,
                        a => a.CategoryId,
                        (a, c) => a.Category = c is null ? null : EntityMappings.ToEntity(c));

                    // Keep a full in-memory copy for filtering
                    _allAssets = assets;

                    // Apply any active filter to populate the visible collection
                    ApplyFilter();
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token expired and refresh failed - go to login
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

    private async Task SearchAssetsAsync()
    {
        // Apply local filter
        ApplyFilter();
        await Task.CompletedTask;
    }

    private void ApplyFilter()
    {
        var list = _allAssets ?? new List<Asset>();

        var results = string.IsNullOrWhiteSpace(SearchText)
            ? list
            : list.Where(a =>
                (!string.IsNullOrEmpty(a.Name) && a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(a.PurchasedFrom) && a.PurchasedFrom.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (a.Category != null && !string.IsNullOrEmpty(a.Category.Title) && a.Category.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        Assets.Clear();
        foreach (var a in results)
        {
            Assets.Add(a);
        }

        UpdateVisibility();
    }

    private async Task OnAssetSelected(Asset? asset)
    {
        if (asset == null)
        {
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "assetId", asset.Id.ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("AssetDetail", parameters),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task AddAssetAsync()
    {
        try
        {
            var param  = new Dictionary<string, object>
            {
                { "assetId", Guid.Empty.ToString() }
            };
            // Navigate to the in-app AssetEdit page without an assetId to create a new asset
            await SafeExecuteAsync(
                () => Shell.Current.GoToAsync("AssetEdit", param),
                onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Fallback: open the web create page in the browser
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
    }
}
