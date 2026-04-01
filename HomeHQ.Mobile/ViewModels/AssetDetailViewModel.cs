using HomeHQ.Mobile.Services;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(AssetId), "assetId")]
public class AssetDetailViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CategoryService _categoryService;
    private readonly SettingsService _settingsService;

    public ObservableCollection<AttributeValue> Attributes { get; } = [];
    public ObservableCollection<Note> Notes { get; } = [];
    public ObservableCollection<Attachment> Attachments { get; } = [];

    public string AssetId
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // Load asset when ID is set
                _ = LoadAssetAsync();
            }
        }
    } = string.Empty;

    public Asset? Asset
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasAsset));
                OnPropertyChanged(nameof(CategoryTitle));
                OnPropertyChanged(nameof(CategoryIcon));
                OnPropertyChanged(nameof(PurchaseDateFormatted));
                OnPropertyChanged(nameof(WarrantyExpirationFormatted));
                OnPropertyChanged(nameof(WarrantyStatusText));
                OnPropertyChanged(nameof(WarrantyStatusColor));
                OnPropertyChanged(nameof(HasWarranty));
            }
        }
    }

    public bool HasAsset => Asset != null;

    public bool IsLoading
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool HasError
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    public string? ErrorMessage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowContent => !IsLoading && !HasError && HasAsset;

    // Computed properties for display
    public string CategoryTitle => Asset?.Category?.Title ?? "Uncategorized";
    public string CategoryIcon => Asset?.Category?.Icon ?? "📦";
    public string PurchaseDateFormatted => Asset?.PurchaseDate.HasValue == true
        ? Formatter.FormatDate(Asset.PurchaseDate, "D")
        : "Not specified";
    public string WarrantyExpirationFormatted => Asset?.WarrantyExpiration.HasValue == true
        ? Formatter.FormatDate(Asset.WarrantyExpiration, "D")
        : "Not specified";
    public string WarrantyStatusText => Formatter.FormatWarrantyStatus(Asset?.WarrantyExpiration);
    public bool HasWarranty => Asset?.WarrantyExpiration.HasValue == true;

    public Color WarrantyStatusColor
    {
        get
        {
            if (Asset?.WarrantyExpiration == null)
            {
                return Colors.Gray;
            }

            var daysRemaining = (Asset.WarrantyExpiration.Value - DateTime.Today).Days;
            return daysRemaining switch
            {
                < 0 => Colors.Gray,
                <= 30 => Colors.Orange,
                _ => Colors.Green
            };
        }
    }

    public bool HasAttributes => Attributes.Count > 0;
    public bool HasNotes => Notes.Count > 0;
    public bool HasAttachments => Attachments.Count > 0;

    public ICommand GoBackCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenAttachmentCommand { get; }

    public AssetDetailViewModel(ApiClient apiClient, CategoryService categoryService, SettingsService settingsService)
    {
        _apiClient = apiClient;
        _categoryService = categoryService;
        _settingsService = settingsService;

        GoBackCommand = new Command(async () => await GoBackAsync());
        EditCommand = new Command(async () => await EditAssetAsync());
        DeleteCommand = new Command(async () => await DeleteAssetAsync());
        RefreshCommand = new Command(async () => await LoadAssetAsync());
        OpenAttachmentCommand = new Command<Attachment>(async (attachment) => await OpenAttachmentAsync(attachment));
    }

    public async Task LoadAssetAsync()
    {
        if (string.IsNullOrEmpty(AssetId))
        {
            return;
        }

        if (!Guid.TryParse(AssetId, out var assetGuid))
        {
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            // Ensure categories are loaded
            await _categoryService.EnsureLoadedAsync();

            // Load asset
            var response = await _apiClient.GetAsync($"api/assets/{AssetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Asset>>();
                if (apiResponse?.Data != null)
                {
                    Asset = apiResponse.Data;
                    _categoryService.PopulateCategory(Asset);
                }
                else
                {
                    HasError = true;
                    ErrorMessage = "Asset not found";
                    return;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }
            else
            {
                HasError = true;
                ErrorMessage = $"Failed to load asset: {response.StatusCode}";
                return;
            }

            // Load attributes, notes, and attachments in parallel
            var attributesTask = LoadAttributesAsync(assetGuid);
            var notesTask = LoadNotesAsync(assetGuid);
            var attachmentsTask = LoadAttachmentsAsync(assetGuid);

            await Task.WhenAll(attributesTask, notesTask, attachmentsTask);

            OnPropertyChanged(nameof(HasAttributes));
            OnPropertyChanged(nameof(HasNotes));
            OnPropertyChanged(nameof(HasAttachments));
        }
        catch (HttpRequestException ex)
        {
            HasError = true;
            ErrorMessage = $"Connection error: {ex.Message}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAttributesAsync(Guid assetId)
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/attributevalues/by-parent/Asset/{assetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<AttributeValue>>>();
                Attributes.Clear();
                if (apiResponse?.Data != null)
                {
                    foreach (var attr in apiResponse.Data)
                    {
                        Attributes.Add(attr);
                    }
                }
            }
        }
        catch
        {
            // Silently fail for secondary data
        }
    }

    private async Task LoadNotesAsync(Guid assetId)
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/notes/by-parent/Asset/{assetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<Note>>>();
                Notes.Clear();
                if (apiResponse?.Data != null)
                {
                    foreach (var note in apiResponse.Data)
                    {
                        Notes.Add(note);
                    }
                }
            }
        }
        catch
        {
            // Silently fail for secondary data
        }
    }

    private async Task LoadAttachmentsAsync(Guid assetId)
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/attachments/by-parent/Asset/{assetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<Attachment>>>();
                Attachments.Clear();
                if (apiResponse?.Data != null)
                {
                    foreach (var attachment in apiResponse.Data)
                    {
                        Attachments.Add(attachment);
                    }
                }
            }
        }
        catch
        {
            // Silently fail for secondary data
        }
    }

    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task EditAssetAsync()
    {
        if (Asset == null)
        {
            return;
        }

        // Open the web edit page in the browser
        var editUrl = $"{_settingsService.ApiBaseUrl}/assets/{Asset.Id}/edit";
        await Browser.OpenAsync(editUrl, BrowserLaunchMode.External);
    }

    private async Task DeleteAssetAsync()
    {
        if (Asset == null)
        {
            return;
        }

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Delete Asset",
            $"Are you sure you want to delete '{Asset.Name}'? This action cannot be undone.",
            "Delete", "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            var response = await _apiClient.DeleteAsync($"api/assets/{Asset.Id}");
            if (response.IsSuccessStatusCode)
            {
                await Shell.Current.DisplayAlertAsync("Success", "Asset deleted successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "Failed to delete asset", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Error deleting asset: {ex.Message}", "OK");
        }
    }

    private async Task OpenAttachmentAsync(Attachment? attachment)
    {
        if (attachment == null)
        {
            return;
        }

        // Navigate to attachment viewer page
        await Shell.Current.GoToAsync($"AttachmentViewer?attachmentId={attachment.Id}");
    }
}
