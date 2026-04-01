using HomeHQ.Mobile.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.ViewModels;

public class AssetsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly CategoryService _categoryService;
    private readonly SettingsService _settingsService;

    public ObservableCollection<Asset> Assets { get; } = [];
    public string SearchText
    {
        get;
        set => SetProperty(ref field, value);
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
    public ICommand LogoutCommand { get; }
    public ICommand AddAssetCommand { get; }

    public AssetsViewModel(ApiClient apiClient, AuthService authService, CategoryService categoryService, SettingsService settingsService)
    {
        _apiClient = apiClient;
        _authService = authService;
        _categoryService = categoryService;
        _settingsService = settingsService;

        RefreshCommand = new Command(async () => await LoadAssetsAsync(forceRefresh: true));
        SearchCommand = new Command(async () => await SearchAssetsAsync());
        AssetSelectedCommand = new Command<Asset>(async (asset) => await OnAssetSelected(asset));
        LogoutCommand = new Command(async () => await LogoutAsync());
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
            await _categoryService.EnsureLoadedAsync(forceRefresh);

            var response = await _apiClient.GetAsync("api/assets");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<Asset>>>();

                Assets.Clear();
                
                if (apiResponse?.Data?.Count > 0)
                {
                    // Populate categories from shared service
                    _categoryService.PopulateCategories(apiResponse.Data);
                    
                    foreach (var asset in apiResponse.Data)
                    {
                        Assets.Add(asset);
                    }
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
        // Filter locally for now, or implement server-side search
        await LoadAssetsAsync();
    }

    private async Task OnAssetSelected(Asset? asset)
    {
        if (asset == null)
        {
            return;
        }

        // Navigate to asset detail page
        await Shell.Current.GoToAsync($"//AssetDetail?assetId={asset.Id}");
    }

    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Logout",
            "Are you sure you want to logout?",
            "Yes", "No");

        if (confirm)
        {
            _authService.Logout();
            _categoryService.ClearCache();
            await Shell.Current.GoToAsync("//Login");
        }
    }

    private async Task AddAssetAsync()
    {
        // Open the web create page in the browser
        var createUrl = $"{_settingsService.ApiBaseUrl}/assets/create";
        await Browser.OpenAsync(createUrl, BrowserLaunchMode.External);
    }

    private void UpdateVisibility()
    {
        ShowEmptyState = !IsLoading && !HasError && Assets.Count == 0;
        ShowList = !IsLoading && !HasError && Assets.Count > 0;
    }
}
