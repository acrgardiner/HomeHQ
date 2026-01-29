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

    public ObservableCollection<Asset> Assets { get; } = [];

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetProperty(ref _isLoading, value);
            UpdateVisibility();
        }
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
            UpdateVisibility();
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private bool _showEmptyState;
    public bool ShowEmptyState
    {
        get => _showEmptyState;
        set => SetProperty(ref _showEmptyState, value);
    }

    private bool _showList;
    public bool ShowList
    {
        get => _showList;
        set => SetProperty(ref _showList, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AssetSelectedCommand { get; }
    public ICommand LogoutCommand { get; }

    public AssetsViewModel(ApiClient apiClient, AuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;

        RefreshCommand = new Command(async () => await LoadAssetsAsync(forceRefresh: true));
        SearchCommand = new Command(async () => await SearchAssetsAsync());
        AssetSelectedCommand = new Command<Asset>(async (asset) => await OnAssetSelected(asset));
        LogoutCommand = new Command(async () => await LogoutAsync());
    }

    public async Task LoadAssetsAsync(bool forceRefresh = false)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = !forceRefresh;
            IsRefreshing = forceRefresh;
            ErrorMessage = null;

            var response = await _apiClient.GetAsync("api/assets");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<Asset>>>();

                Assets.Clear();
                
                if (apiResponse?.Data?.Count > 0)
                {
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
        if (asset == null) return;

        // Navigate to asset detail page (you can implement this later)
        await Shell.Current.DisplayAlertAsync("Asset Selected", $"You selected: {asset.Name}", "OK");
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
            await Shell.Current.GoToAsync("//Login");
        }
    }

    private void UpdateVisibility()
    {
        ShowEmptyState = !IsLoading && !HasError && Assets.Count == 0;
        ShowList = !IsLoading && !HasError && Assets.Count > 0;
    }
}