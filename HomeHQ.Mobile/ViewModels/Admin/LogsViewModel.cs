using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels.Admin;

public class LogsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private List<LogEntryItem> _allEntries = [];

    public ObservableCollection<string> LogFiles { get; } = [];
    public ObservableCollection<string> Levels { get; } = ["All", "Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];
    public ObservableCollection<LogEntryItem> Entries { get; } = [];

    public string SelectedLogFile
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && !string.IsNullOrWhiteSpace(value))
            {
                _ = LoadEntriesAsync();
            }
        }
    } = string.Empty;

    public string SelectedLevel
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = "All";

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
    public ICommand EntrySelectedCommand { get; }

    public LogsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new Command(async () => await LoadAsync(forceRefresh: true));
        EntrySelectedCommand = new Command<LogEntryItem>(async entry => await OnEntrySelectedAsync(entry));
    }

    public async Task LoadAsync(bool forceRefresh = false)
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

            var response = await _apiClient.GetAsync("api/admin/logs");
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                or System.Net.HttpStatusCode.Forbidden)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                ErrorMessage = apiResponse?.Errors.FirstOrDefault() ?? "Failed to load log files.";
                return;
            }

            LogFiles.Clear();
            foreach (var file in apiResponse.Data)
            {
                LogFiles.Add(file);
            }

            if (LogFiles.Count == 0)
            {
                _allEntries = [];
                ApplyFilter();
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedLogFile) || !LogFiles.Contains(SelectedLogFile))
            {
                SelectedLogFile = LogFiles[0];
            }
            else
            {
                await LoadEntriesAsync();
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

    private async Task LoadEntriesAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFile))
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var response = await _apiClient.GetAsync($"api/admin/logs/{Uri.EscapeDataString(SelectedLogFile)}");
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                or System.Net.HttpStatusCode.Forbidden)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<LogEntryDto>>>();
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                ErrorMessage = apiResponse?.Errors.FirstOrDefault() ?? "Failed to load log entries.";
                return;
            }

            _allEntries = apiResponse.Data.Select(e => new LogEntryItem
            {
                Timestamp = e.Timestamp,
                Level = e.Level,
                SourceContext = e.SourceContext,
                Message = e.Message,
                Exception = e.Exception
            }).ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            UpdateVisibility();
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<LogEntryItem> results = _allEntries;

        if (!string.Equals(SelectedLevel, "All", StringComparison.OrdinalIgnoreCase))
        {
            results = results.Where(e => e.Level == SelectedLevel);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            results = results.Where(e =>
                e.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || e.SourceContext.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        Entries.Clear();
        foreach (var entry in results.Take(500))
        {
            Entries.Add(entry);
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        ShowEmptyState = !IsLoading && !HasError && Entries.Count == 0;
        ShowList = !IsLoading && !HasError && Entries.Count > 0;
    }

    private static async Task OnEntrySelectedAsync(LogEntryItem? entry)
    {
        if (entry is null)
        {
            return;
        }

        var details = $"{entry.Timestamp}\n{entry.Level}\n{entry.SourceContext}\n\n{entry.Message}";
        if (entry.HasException)
        {
            details += $"\n\n{entry.Exception}";
        }

        await Shell.Current.DisplayAlertAsync("Log Entry", details, "OK");
    }
}
