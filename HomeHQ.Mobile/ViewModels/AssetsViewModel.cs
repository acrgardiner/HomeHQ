using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public class AssetsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;

    public ObservableCollection<AssetItemViewModel> Assets { get; } = [];

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
        AssetSelectedCommand = new Command<AssetItemViewModel>(async (asset) => await OnAssetSelected(asset));
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
                var assets = await response.Content.ReadFromJsonAsync<List<AssetDto>>();
                
                Assets.Clear();
                
                if (assets is { Count: > 0 })
                {
                    foreach (var asset in assets)
                    {
                        Assets.Add(new AssetItemViewModel(asset));
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

    private async Task OnAssetSelected(AssetItemViewModel? asset)
    {
        if (asset == null) return;

        // Navigate to asset detail page (you can implement this later)
        await Shell.Current.DisplayAlert("Asset Selected", $"You selected: {asset.Name}", "OK");
    }

    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
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

// DTO to match your server's Asset model
public class AssetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
}

// ViewModel for individual asset items in the list
public class AssetItemViewModel
{
    private readonly AssetDto _asset;

    public AssetItemViewModel(AssetDto asset)
    {
        _asset = asset;
    }

    public int Id => _asset.Id;
    public string Name => _asset.Name;
    public string? Description => _asset.Description;
    public string? Location => _asset.Location;
    public string CategoryName => _asset.CategoryName ?? "Uncategorized";
    public bool HasLocation => !string.IsNullOrEmpty(Location);

    public string CategoryIcon => CategoryName?.ToLowerInvariant() switch
    {
        "electronics" => "📱",
        "furniture" => "🪑",
        "appliances" => "🔌",
        "tools" => "🔧",
        "clothing" => "👕",
        "documents" => "📄",
        "vehicles" => "🚗",
        "jewelry" => "💎",
        _ => "📦"
    };
}
