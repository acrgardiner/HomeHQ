using System.Collections.ObjectModel;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

/// <summary>
/// Non-generic surface for Setup list pages (compiled bindings / shared XAML).
/// </summary>
public abstract class SetupListViewModel : BaseViewModel
{
    public ObservableCollection<SetupListItem> Items { get; } = [];

    protected List<SetupListItem> AllItems { get; set; } = [];

    public string PageTitle
    {
        get;
        protected set => SetProperty(ref field, value);
    } = string.Empty;

    public string SearchPlaceholder
    {
        get;
        protected set => SetProperty(ref field, value);
    } = "Search...";

    public string EmptyTitle
    {
        get;
        protected set => SetProperty(ref field, value);
    } = "No items found";

    public string EmptyMessage
    {
        get;
        protected set => SetProperty(ref field, value);
    } = "Add your first item to get started";

    public string AddButtonText
    {
        get;
        protected set => SetProperty(ref field, value);
    } = "Add";

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
    public ICommand ItemSelectedCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }

    protected SetupListViewModel()
    {
        RefreshCommand = new Command(async () => await LoadAsync(forceRefresh: true));
        SearchCommand = new Command(ApplyFilter);
        ItemSelectedCommand = new Command<SetupListItem>(async item => await OnItemSelectedAsync(item));
        AddCommand = new Command(async () => await OnAddAsync());
        DeleteCommand = new Command<SetupListItem>(async item => await OnDeleteAsync(item));
    }

    public abstract Task LoadAsync(bool forceRefresh = false);

    protected abstract string EditKind { get; }

    protected abstract Task<bool> DeleteItemAsync(Guid id);

    protected void ApplyFilter()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? AllItems
            : AllItems.Where(i =>
                i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(i.Subtitle) && i.Subtitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        Items.Clear();
        foreach (var item in results)
        {
            Items.Add(item);
        }

        UpdateVisibility();
    }

    protected void UpdateVisibility()
    {
        ShowEmptyState = !IsLoading && !HasError && Items.Count == 0;
        ShowList = !IsLoading && !HasError && Items.Count > 0;
    }

    private async Task OnItemSelectedAsync(SetupListItem? item)
    {
        if (item == null)
        {
            return;
        }

        await NavigateToEditAsync(item.Id);
    }

    private async Task OnAddAsync() => await NavigateToEditAsync(Guid.Empty);

    private async Task NavigateToEditAsync(Guid id)
    {
        var parameters = new Dictionary<string, object>
        {
            { "kind", EditKind },
            { "id", id.ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("SetupEdit", parameters),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task OnDeleteAsync(SetupListItem? item)
    {
        if (item == null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete",
            $"Delete '{item.Name}'?",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var success = await DeleteItemAsync(item.Id);
            if (!success)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Failed to delete {item.Name}.", "OK");
                return;
            }

            await LoadAsync(forceRefresh: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Generic Setup list ViewModel backed by <see cref="CacheService{TDto}"/> and REST CRUD.
/// </summary>
public abstract class SetupListViewModel<TDto> : SetupListViewModel
    where TDto : class, IEntityDto
{
    private readonly ApiClient _apiClient;
    private readonly CacheService<TDto> _cache;

    protected SetupListViewModel(ApiClient apiClient, CacheService<TDto> cache)
    {
        _apiClient = apiClient;
        _cache = cache;
    }

    protected abstract string Endpoint { get; }

    protected abstract SetupListItem MapItem(TDto dto);

    public override async Task LoadAsync(bool forceRefresh = false)
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

            var data = await _cache.GetAllAsync(forceRefresh);
            AllItems = data.Select(MapItem).OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
            ApplyFilter();
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

    protected override async Task<bool> DeleteItemAsync(Guid id)
    {
        var response = await _apiClient.DeleteAsync($"{Endpoint}/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await Shell.Current.GoToAsync("//Login");
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        _cache.ClearCache();
        return true;
    }
}
