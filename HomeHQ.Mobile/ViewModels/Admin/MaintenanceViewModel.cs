using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels.Admin;

public class MaintenanceViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    public ObservableCollection<SoftDeletedCountItem> SoftDeletedCounts { get; } = [];

    public bool IsLoadingCounts
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsImporting
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsRebuilding
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsPurging
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowPurgeDetails
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(PurgeToggleText));
                if (value && SoftDeletedCounts.Count == 0)
                {
                    _ = RefreshCountsAsync();
                }
            }
        }
    }

    public string PurgeToggleText => ShowPurgeDetails ? "Hide ▲" : "Show ▼";

    public int SoftDeletedTotal
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool HasSoftDeleted => SoftDeletedTotal > 0;

    public string? StatusMessage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand ImportCommand { get; }
    public ICommand RebuildThumbnailsCommand { get; }
    public ICommand RefreshCountsCommand { get; }
    public ICommand PurgeCommand { get; }
    public ICommand TogglePurgeCommand { get; }

    public MaintenanceViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        ImportCommand = new Command(async () => await ImportAsync(), () => !IsImporting);
        RebuildThumbnailsCommand = new Command(async () => await RebuildThumbnailsAsync(), () => !IsRebuilding);
        RefreshCountsCommand = new Command(async () => await RefreshCountsAsync());
        PurgeCommand = new Command(async () => await PurgeAsync(), () => !IsPurging);
        TogglePurgeCommand = new Command(() => ShowPurgeDetails = !ShowPurgeDetails);
    }

    public async Task LoadAsync()
    {
        await RefreshCountsAsync();
    }

    private async Task ImportAsync()
    {
        try
        {
            IsImporting = true;
            StatusMessage = "Importing assets...";

            var response = await _apiClient.PostAsync("api/admin/import");
            if (await HandleAuthFailureAsync(response))
            {
                return;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<int>>();
            if (apiResponse?.Success == true)
            {
                StatusMessage = apiResponse.Message ?? $"Imported {apiResponse.Data} assets.";
                await Shell.Current.DisplayAlertAsync("Success", StatusMessage, "OK");
            }
            else
            {
                await ShowErrorAsync(apiResponse?.Errors.FirstOrDefault() ?? "Import failed.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsImporting = false;
        }
    }

    private async Task RebuildThumbnailsAsync()
    {
        try
        {
            IsRebuilding = true;
            StatusMessage = "Rebuilding thumbnails...";

            var response = await _apiClient.PostAsync("api/admin/thumbnails/rebuild");
            if (await HandleAuthFailureAsync(response))
            {
                return;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<int>>();
            if (apiResponse?.Success == true)
            {
                StatusMessage = apiResponse.Message ?? $"Rebuilt {apiResponse.Data} thumbnails.";
                await Shell.Current.DisplayAlertAsync("Success", StatusMessage, "OK");
            }
            else
            {
                await ShowErrorAsync(apiResponse?.Errors.FirstOrDefault() ?? "Rebuild failed.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsRebuilding = false;
        }
    }

    private async Task RefreshCountsAsync()
    {
        try
        {
            IsLoadingCounts = true;

            var response = await _apiClient.GetAsync("api/admin/purge/counts");
            if (await HandleAuthFailureAsync(response))
            {
                return;
            }

            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<SoftDeletedCountDto>>>();
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                StatusMessage = apiResponse?.Errors.FirstOrDefault() ?? "Failed to load purge counts.";
                return;
            }

            SoftDeletedCounts.Clear();
            foreach (var item in apiResponse.Data)
            {
                SoftDeletedCounts.Add(new SoftDeletedCountItem
                {
                    EntityType = item.EntityType,
                    Count = item.Count
                });
            }

            SoftDeletedTotal = apiResponse.Data.Sum(x => x.Count);
            OnPropertyChanged(nameof(HasSoftDeleted));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading counts: {ex.Message}";
        }
        finally
        {
            IsLoadingCounts = false;
        }
    }

    private async Task PurgeAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Confirm Purge All",
            $"Are you sure you want to permanently delete all {SoftDeletedTotal} soft-deleted records?\n\nThis action cannot be undone!",
            "Yes, Purge All",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            IsPurging = true;
            StatusMessage = "Purging...";

            var response = await _apiClient.PostAsync("api/admin/purge");
            if (await HandleAuthFailureAsync(response))
            {
                return;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<SoftDeletedCountDto>>>();
            if (apiResponse?.Success == true)
            {
                StatusMessage = apiResponse.Message ?? "Purge completed.";
                await Shell.Current.DisplayAlertAsync("Success", StatusMessage, "OK");
                await RefreshCountsAsync();
            }
            else
            {
                await ShowErrorAsync(apiResponse?.Errors.FirstOrDefault() ?? "Purge failed.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsPurging = false;
        }
    }

    private static async Task<bool> HandleAuthFailureAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden)
        {
            await Shell.Current.GoToAsync("//Login");
            return true;
        }

        return false;
    }

    private async Task ShowErrorAsync(string message)
    {
        StatusMessage = message;
        await Shell.Current.DisplayAlertAsync("Error", message, "OK");
    }
}
