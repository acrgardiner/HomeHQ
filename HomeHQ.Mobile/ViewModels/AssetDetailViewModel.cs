using HomeHQ.Mobile.Services;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(Refresh), "refresh")]
[QueryProperty(nameof(AssetId), "assetId")]
public class AssetDetailViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CacheService<Category> _categoryCache;
    private readonly CacheService<WarrantyType> _warrantyTypeCache;
    private readonly CacheService<AttachmentType> _attachmentTypeCache;
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
                //_ = LoadAssetAsync();
            }
        }
    } = string.Empty;

    public bool Refresh { get; set; } = false;

    public Asset? Asset
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasAsset));
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

    // --- Attachment Carousel ---

    private int _currentAttachmentIndex;

    /// <summary>The attachment currently shown in the carousel.</summary>
    public Attachment? CurrentAttachment
        => Attachments.Count > 0 && _currentAttachmentIndex < Attachments.Count
               ? Attachments[_currentAttachmentIndex]
               : null;

    /// <summary>Zero-based index of the currently displayed attachment.</summary>
    public int CurrentAttachmentIndex
    {
        get => _currentAttachmentIndex;
        set
        {
            var clamped = Math.Clamp(value, 0, Math.Max(0, Attachments.Count - 1));
            if (SetProperty(ref _currentAttachmentIndex, clamped))
            {
                NotifyAttachmentCarouselChanged();
                _ = LoadCurrentAttachmentPreviewAsync();
            }
        }
    }

    public bool CanGoPreviousAttachment => _currentAttachmentIndex > 0;
    public bool CanGoNextAttachment     => _currentAttachmentIndex < Attachments.Count - 1;

    public string AttachmentCountText => Attachments.Count > 0
        ? $"{_currentAttachmentIndex + 1} of {Attachments.Count}"
        : string.Empty;

    public bool HasMultipleAttachments => Attachments.Count > 1;

    public bool CurrentAttachmentIsImage
        => CurrentAttachment?.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;

    public string CurrentAttachmentFileIcon => GetFileTypeIcon(CurrentAttachment?.ContentType);

    /// <summary>Image bytes loaded from the API for the current attachment (images only).</summary>
    public ImageSource? CurrentAttachmentPreviewSource
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasPreviewImage));
                OnPropertyChanged(nameof(ShowFileIcon));
            }
        }
    }

    /// <summary>True while the preview image is being fetched from the API.</summary>
    public bool IsLoadingPreview
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ShowFileIcon));
            }
        }
    }

    /// <summary>True when a preview image has been successfully loaded.</summary>
    public bool HasPreviewImage => CurrentAttachmentPreviewSource != null;

    /// <summary>
    /// True when the file-icon fallback should be shown (no image loaded and not currently loading).
    /// </summary>
    public bool ShowFileIcon => !IsLoadingPreview && !HasPreviewImage;

    /// <summary>Human-readable file size of the current attachment (e.g. "1.4 MB").</summary>
    public string CurrentAttachmentFileSizeFormatted
        => Formatter.FormatFileSize(CurrentAttachment?.FileSize ?? 0);

    /// <summary>True when the current attachment has a classified type.</summary>
    public bool CurrentAttachmentHasType
        => CurrentAttachment?.AttachmentTypeId != null
           && _attachmentTypeCache.GetById(CurrentAttachment.AttachmentTypeId) != null;

    public ICommand PreviousAttachmentCommand { get; }
    public ICommand NextAttachmentCommand { get; }
    public ICommand SelectAttachmentCommand { get; }

    public AssetDetailViewModel(ApiClient apiClient
        , CacheService<Category> categoryCache
        , CacheService<AttachmentType> attachmentTypeCache
        , CacheService<WarrantyType> warrantyTypeCache
        , SettingsService settingsService
    )
    {
        _apiClient = apiClient;

        _categoryCache = categoryCache;
        _attachmentTypeCache = attachmentTypeCache;
        _warrantyTypeCache = warrantyTypeCache;

        _settingsService = settingsService;

        GoBackCommand = new Command(async () => await GoBackAsync());
        EditCommand = new Command(async () => await EditAssetAsync());
        DeleteCommand = new Command(async () => await DeleteAssetAsync());
        RefreshCommand = new Command(async () => await LoadAssetAsync());
        OpenAttachmentCommand = new Command<Attachment>(async (attachment) => await OpenAttachmentAsync(attachment));

        PreviousAttachmentCommand = new Command(() => CurrentAttachmentIndex--);
        NextAttachmentCommand     = new Command(() => CurrentAttachmentIndex++);
        SelectAttachmentCommand   = new Command<Attachment>(attachment =>
        {
            var index = Attachments.IndexOf(attachment);
            if (index >= 0)
            {
                CurrentAttachmentIndex = index;
            }
        });
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

            // Ensure reference caches are warm before resolving navigation properties.
            await Task.WhenAll(
                _categoryCache.EnsureLoadedAsync(),
                _attachmentTypeCache.EnsureLoadedAsync(),
                _warrantyTypeCache.EnsureLoadedAsync()
            );

            // Load asset
            var response = await _apiClient.GetAsync($"api/assets/{AssetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Asset>>();
                if (apiResponse?.Data != null)
                {
                    // Populate Category navigation property
                    _categoryCache.Populate(
                        apiResponse.Data,
                        a => a.CategoryId,
                        (a, c) => a.Category = c);

                    _warrantyTypeCache.Populate(
                        apiResponse.Data,
                        a => a.WarrantyTypeId,
                        (a, c) => a.WarrantyType = c);

                    Asset = apiResponse.Data;
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
                    _attachmentTypeCache.PopulateAll(
                        apiResponse.Data,
                        a => a.AttachmentTypeId,
                        (a, c) => a.AttachmentType = c);

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
        finally
        {
            // Reset carousel to first item whenever attachments are (re)loaded.
            // Directly assign the backing field so the setter's equality guard
            // does not prevent re-initialisation on subsequent reloads.
            _currentAttachmentIndex = 0;
            NotifyAttachmentCarouselChanged();
            CurrentAttachmentPreviewSource = null;
            IsLoadingPreview = false;
            if (Attachments.Count > 0)
            {
                _ = LoadCurrentAttachmentPreviewAsync();
            }
        }
    }

    /// <summary>Fires all property-change notifications related to the carousel state.</summary>
    private void NotifyAttachmentCarouselChanged()
    {
        OnPropertyChanged(nameof(CurrentAttachment));
        OnPropertyChanged(nameof(CurrentAttachmentIndex));
        OnPropertyChanged(nameof(CanGoPreviousAttachment));
        OnPropertyChanged(nameof(CanGoNextAttachment));
        OnPropertyChanged(nameof(AttachmentCountText));
        OnPropertyChanged(nameof(HasMultipleAttachments));
        OnPropertyChanged(nameof(CurrentAttachmentIsImage));
        OnPropertyChanged(nameof(CurrentAttachmentFileIcon));
        OnPropertyChanged(nameof(CurrentAttachmentFileSizeFormatted));
        OnPropertyChanged(nameof(CurrentAttachmentHasType));
        OnPropertyChanged(nameof(HasAttachments));
    }

    /// <summary>
    /// Downloads the raw bytes for the current attachment from the API and
    /// exposes them as an <see cref="ImageSource"/> (images only).
    /// </summary>
    private async Task LoadCurrentAttachmentPreviewAsync()
    {
        CurrentAttachmentPreviewSource = null;

        if (CurrentAttachment == null || !CurrentAttachmentIsImage)
        {
            return;
        }

        try
        {
            IsLoadingPreview = true;
            //Check local cache first
            var localFilePath = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), nameof(Asset), AssetId, CurrentAttachment.LocalFileName);
            if (File.Exists(localFilePath))
            {
                CurrentAttachmentPreviewSource = ImageSource.FromFile(localFilePath);
            }
            else
            {
                var response = await _apiClient.GetAsync($"api/attachments/{CurrentAttachment.Id}/data");
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    CurrentAttachmentPreviewSource = ImageSource.FromStream(() => new MemoryStream(bytes));

                    // Cache the file locally for future preview loads
                    Directory.CreateDirectory(Path.GetDirectoryName(localFilePath) ?? string.Empty);
                    await File.WriteAllBytesAsync(localFilePath, bytes);
                }
            }
        }
        catch
        {
            // Preview load failed silently; ShowFileIcon will fall back to the icon.
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    /// <summary>Returns an emoji icon that represents the given MIME content type.</summary>
    private static string GetFileTypeIcon(string? contentType) => contentType switch
    {
        var ct when ct?.StartsWith("image/") == true                                          => "🖼️",
        var ct when ct?.Contains("pdf") == true                                               => "📄",
        var ct when ct?.Contains("word") == true                                              => "📝",
        var ct when ct?.Contains("excel") == true || ct?.Contains("spreadsheet") == true     => "📊",
        var ct when ct?.Contains("video") == true                                             => "🎬",
        var ct when ct?.Contains("audio") == true                                             => "🎵",
        var ct when ct?.Contains("zip") == true || ct?.Contains("compressed") == true        => "📦",
        _                                                                                     => "📎"
    };

    private async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            // Surface the full exception so the root cause is visible
            await Shell.Current.DisplayAlertAsync(
                "Navigation Error",
                $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "OK");
        }
    }

    private async Task EditAssetAsync()
    {
        if (Asset == null)
        {
            return;
        }

        var param = new Dictionary<string, object>
        {
            { "assetId", Asset.Id.ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync($"AssetEdit", param),
            onError: ex =>
            {
                HasError = true;
                ErrorMessage = $"Could not open edit page: {ex.Message}";
            });
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

        var param = new Dictionary<string, object>
        {
            { "attachmentId", attachment.Id.ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("AttachmentViewer", param),
            onError: ex => ErrorMessage = $"Could not open attachment: {ex.Message}");
    }
}
