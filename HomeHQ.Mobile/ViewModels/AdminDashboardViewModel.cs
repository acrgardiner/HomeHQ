using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Helpers;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public class AdminDashboardViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    public ObservableCollection<LargeAttachmentItem> LargestAttachments { get; } = [];
    public ObservableCollection<ExpiringWarrantyItem> WarrantiesExpiringSoon { get; } = [];
    public ObservableCollection<SoftDeletedCountItem> PendingPurge { get; } = [];

    public int UserCount
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int AttachmentCount
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string TotalAttachmentSizeDisplay
    {
        get;
        set => SetProperty(ref field, value);
    } = "0 B";

    public int PendingPurgeTotal
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
    public bool HasLargestAttachments => LargestAttachments.Count > 0;
    public bool HasWarrantiesExpiring => WarrantiesExpiringSoon.Count > 0;
    public bool HasPendingPurge => PendingPurge.Count > 0;

    public ICommand RefreshCommand { get; }
    public ICommand WarrantySelectedCommand { get; }
    public ICommand AttachmentSelectedCommand { get; }

    public AdminDashboardViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new Command(async () => await LoadAsync(forceRefresh: true));
        WarrantySelectedCommand = new Command<ExpiringWarrantyItem>(async item => await OnWarrantySelectedAsync(item));
        AttachmentSelectedCommand = new Command<LargeAttachmentItem>(async item => await OnAttachmentSelectedAsync(item));
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

            var response = await _apiClient.GetAsync("api/admin/dashboard");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AdminDashboardDto>>();
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                ErrorMessage = apiResponse?.Errors.FirstOrDefault() ?? "Failed to load admin dashboard.";
                return;
            }

            var data = apiResponse.Data;
            UserCount = data.UserCount;
            AttachmentCount = data.AttachmentCount;
            TotalAttachmentSizeDisplay = Formatter.FormatFileSize(data.TotalAttachmentSize);

            LargestAttachments.Clear();
            foreach (var item in data.LargestAttachments)
            {
                LargestAttachments.Add(new LargeAttachmentItem
                {
                    Id = item.Id,
                    OriginFileName = item.OriginFileName,
                    FileSizeDisplay = Formatter.FormatFileSize(item.FileSize),
                    ParentId = item.ParentId,
                    ParentName = item.ParentName ?? "—"
                });
            }

            WarrantiesExpiringSoon.Clear();
            foreach (var item in data.WarrantiesExpiringSoon)
            {
                WarrantiesExpiringSoon.Add(new ExpiringWarrantyItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    WarrantyExpirationDisplay = Formatter.FormatDate(item.WarrantyExpiration),
                    CategoryName = item.CategoryName ?? "—",
                    WarrantyTypeName = item.WarrantyTypeName ?? "—"
                });
            }

            PendingPurge.Clear();
            foreach (var item in data.PendingPurge.Where(p => p.Count > 0))
            {
                PendingPurge.Add(new SoftDeletedCountItem
                {
                    EntityType = item.EntityType,
                    Count = item.Count
                });
            }

            PendingPurgeTotal = data.PendingPurge.Sum(p => p.Count);
            OnPropertyChanged(nameof(HasLargestAttachments));
            OnPropertyChanged(nameof(HasWarrantiesExpiring));
            OnPropertyChanged(nameof(HasPendingPurge));
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

    private async Task OnWarrantySelectedAsync(ExpiringWarrantyItem? item)
    {
        if (item is null)
        {
            return;
        }

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("AssetDetail", new Dictionary<string, object>
            {
                { "assetId", item.Id.ToString() }
            }),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task OnAttachmentSelectedAsync(LargeAttachmentItem? item)
    {
        if (item?.ParentId is null || item.ParentId == Guid.Empty)
        {
            return;
        }

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("AssetDetail", new Dictionary<string, object>
            {
                { "assetId", item.ParentId.Value.ToString() }
            }),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }
}
