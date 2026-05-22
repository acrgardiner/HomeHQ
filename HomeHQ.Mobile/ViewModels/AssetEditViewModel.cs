using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Helpers;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;
using Microsoft.Maui.ApplicationModel;
using Attachment = HomeHQ.Entities.Attachment;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(AssetId), "assetId")]
[QueryProperty(nameof(ShareMimeType), "shareMimeType")]
public class AssetEditViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CacheService<CategoryDto> _categoryCache;
    private readonly CacheService<WarrantyTypeDto> _warrantyTypeCache;
    private readonly CacheService<AttachmentTypeDto> _attachmentTypeCache;
    private readonly AttachmentViewerNavigation _attachmentViewerNavigation;

    // The raw loaded asset (kept for Id reference during save)
    private Asset? _loadedAsset;

    // ── Query property ─────────────────────────────────────────────────────────

    public string AssetId
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _ = LoadAsync();
            }
        }
    } = string.Empty;

    /// <summary>Android share intent MIME type (e.g. image/jpeg); used to correct <see cref="Attachment.ContentType"/> for staged files.</summary>
    public string ShareMimeType
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyShareMimeToStagedAttachments();
            }
        }
    } = string.Empty;

    // ── State ──────────────────────────────────────────────────────────────────

    public bool IsLoading
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool IsSaving
    {
        get;
        set => SetProperty(ref field, value);
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

    public bool ShowContent => !IsLoading && !HasError;

    public bool IsNew => _loadedAsset == null || _loadedAsset.Id == Guid.Empty;

    public string PageTitle => IsNew ? "New Asset" : "Edit Asset";

    // ── Basic information fields ───────────────────────────────────────────────

    public string Name
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string PurchasedFrom
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Purchase date for the DatePicker (MAUI DatePicker requires non-nullable DateTime).
    /// Defaults to today when the asset has no purchase date.
    /// </summary>
    public DateTime PurchaseDate
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RecalculateWarrantyExpiration();
            }
        }
    } = DateTime.Today;

    // ── Picker data & selection ────────────────────────────────────────────────

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<WarrantyType> WarrantyTypes { get; } = [];
    public ObservableCollection<AttachmentType> AttachmentTypes { get; } = [];

    private Guid _defaultAttachmentTypeId;
    private AttachmentType _defaultAttachmentType;

    private Guid _defaultWarrantyTypeId;
    private WarrantyType _defaultWarrantyType;

    public Category? SelectedCategory
    {
        get;
        set => SetProperty(ref field, value);
    }

    public WarrantyType? SelectedWarrantyType
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsWarrantyExpirationEnabled));
                RecalculateWarrantyExpiration();
            }
        }
    }

    // ── Warranty ───────────────────────────────────────────────────────────────

    public DateTime WarrantyExpiration
    {
        get;
        set => SetProperty(ref field, value);
    } = DateTime.Today;

    /// <summary>True when a warranty type is selected, enabling the expiration date picker.</summary>
    public bool IsWarrantyExpirationEnabled => SelectedWarrantyType != null;

    // ── Attributes & Notes ────────────────────────────────────────────────────

    public ObservableCollection<AttributeViewModel> Attributes { get; } = [];
    public ObservableCollection<Attachment> Attachments { get; } = [];
    public ObservableCollection<NoteViewModel> Notes { get; } = [];

    public bool HasAttributes => Attributes.Count > 0;
    public bool HasNotes => Notes.Count > 0;
    public bool HasAttachments => Attachments.Count > 0;

    // --- Attachment Carousel ---

    /// <summary>The attachment currently shown in the carousel.</summary>
    public Attachment? CurrentAttachment
        => Attachments.Count > 0 && CurrentAttachmentIndex < Attachments.Count
               ? Attachments[CurrentAttachmentIndex]
               : null;

    /// <summary>Zero-based index of the currently displayed attachment.</summary>
    public int CurrentAttachmentIndex
    {
        get;
        set
        {
            var clamped = Math.Clamp(value, 0, Math.Max(0, Attachments.Count - 1));
            if (SetProperty(ref field, clamped))
            {
                NotifyAttachmentCarouselChanged();
                _ = LoadCurrentAttachmentPreviewAsync();
            }
        }
    }

    public bool CanGoPreviousAttachment => CurrentAttachmentIndex > 0;
    public bool CanGoNextAttachment => CurrentAttachmentIndex < Attachments.Count - 1;

    public string AttachmentCountText => Attachments.Count > 0
        ? $"{CurrentAttachmentIndex + 1} of {Attachments.Count}"
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

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand GoBackCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddAttributeCommand { get; }
    public ICommand RemoveAttributeCommand { get; }
    public ICommand AddNoteCommand { get; }
    public ICommand RemoveNoteCommand { get; }
    public ICommand AddAttachmentPhotoCommand { get; }
    public ICommand AddAttachmentGalleryCommand { get; }
    public ICommand RemoveAttachmentCommand { get; }

    public ICommand PreviousAttachmentCommand { get; }
    public ICommand NextAttachmentCommand { get; }
    public ICommand SelectAttachmentCommand { get; }

    public ICommand OpenAttachmentCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────────

    public AssetEditViewModel(
        ApiClient apiClient
        , CacheService<CategoryDto> categoryCache
        , CacheService<AttachmentTypeDto> attachmentTypeCache
        , CacheService<WarrantyTypeDto> warrantyTypeCache
        , AttachmentViewerNavigation attachmentViewerNavigation
    )
    {
        _apiClient = apiClient;

        _categoryCache = categoryCache;
        _warrantyTypeCache = warrantyTypeCache;
        _attachmentTypeCache = attachmentTypeCache;
        _attachmentViewerNavigation = attachmentViewerNavigation;

        GoBackCommand = new Command(async () => await GoBackAsync());
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        AddAttributeCommand = new Command(() =>
        {
            Attributes.Add(new AttributeViewModel(new Entities.Attribute()));
            OnPropertyChanged(nameof(HasAttributes));
        });
        RemoveAttributeCommand = new Command<AttributeViewModel>(async attr => await RemoveAttributeAsync(attr));
        AddNoteCommand = new Command(() =>
        {
            Notes.Add(new NoteViewModel(new Note()));
            OnPropertyChanged(nameof(HasNotes));
        });
        RemoveNoteCommand = new Command<NoteViewModel>(async note => await RemoveNoteAsync(note));
        AddAttachmentPhotoCommand = new Command(async () => await AddAttachmentPhoto());
        AddAttachmentGalleryCommand = new Command(async () => await AddAttachmentGallery());
        RemoveAttachmentCommand = new Command(async () => await RemoveAttachmentAsync());

        PreviousAttachmentCommand = new Command(() => CurrentAttachmentIndex--);
        NextAttachmentCommand = new Command(() => CurrentAttachmentIndex++);
        SelectAttachmentCommand = new Command<Attachment>(attachment =>
        {
            var index = Attachments.IndexOf(attachment);
            if (index >= 0)
            {
                CurrentAttachmentIndex = index;
            }
        });

        OpenAttachmentCommand = new Command<Attachment>(async (attachment) => await OpenAttachmentAsync(attachment));
    }

    // ── Load ───────────────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            // Warm reference caches in parallel
            await Task.WhenAll(
                _categoryCache.EnsureLoadedAsync(),
                _attachmentTypeCache.EnsureLoadedAsync(),
                _warrantyTypeCache.EnsureLoadedAsync());

            await PopulatePickerDataAsync();

            // If no AssetId provided, initialize form for creating a new asset.
            if (string.IsNullOrEmpty(AssetId) || !Guid.TryParse(AssetId, out var assetGuid) || AssetId == Guid.Empty.ToString())
            {
                await InitializeNewAssetAsync();
                return;
            }

            // Load asset
            var response = await _apiClient.GetAsync($"api/assets/{AssetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AssetDto>>();
                if (apiResponse?.Data != null)
                {
                    _loadedAsset = EntityMappings.ToEntity(apiResponse.Data);
                    await PopulateFormFromAsset(_loadedAsset);
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

    private async Task PopulateFormFromAsset(Asset asset)
    {
        _loadedAsset = asset;
        Name = asset.Name ?? string.Empty;
        PurchasedFrom = asset.PurchasedFrom ?? string.Empty;
        PurchaseDate = asset.PurchaseDate ?? DateTime.Today;

        // Resolve selected items
        SelectedCategory = asset.CategoryId.HasValue
            ? Categories.FirstOrDefault(c => c.Id == asset.CategoryId.Value)
            : null;

        SelectedWarrantyType = asset.WarrantyTypeId.HasValue
            ? WarrantyTypes.FirstOrDefault(w => w.Id == asset.WarrantyTypeId.Value)
            : null;

        WarrantyExpiration = asset.WarrantyExpiration ?? DateTime.Today;

        // Notify that PageTitle/IsNew may have changed now that we have a loaded asset
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(PageTitle));
    }

    private async Task PopulatePickerDataAsync()
    {
        // Populate picker collections from cache
        Categories.Clear();
        foreach (var cat in (await _categoryCache.GetAllAsync()))
        {
            Categories.Add(EntityMappings.ToEntity(cat));
        }

        WarrantyTypes.Clear();
        foreach (var wt in (await _warrantyTypeCache.GetAllAsync()).OrderBy(w => w.SortOrder))
        {
            var entity = EntityMappings.ToEntity(wt);
            WarrantyTypes.Add(entity);
            if (wt.Default)
            {
                _defaultWarrantyTypeId = wt.Id;
                _defaultWarrantyType = entity;
            }
        }

        AttachmentTypes.Clear();
        foreach (var at in (await _attachmentTypeCache.GetAllAsync()).OrderBy(w => w.Default))
        {
            var entity = EntityMappings.ToEntity(at);
            AttachmentTypes.Add(entity);
            if (at.Default)
            {
                _defaultAttachmentTypeId = at.Id;
                _defaultAttachmentType = entity;
            }
        }
    }

    private async Task InitializeNewAssetAsync()
    {
        _loadedAsset = new Asset { Id = Guid.Empty };

        // Defaults
        Name = string.Empty;
        PurchasedFrom = string.Empty;
        PurchaseDate = DateTime.Today;
        SelectedCategory = null;
        SelectedWarrantyType = _defaultWarrantyType;

        //If files exist in Guid.Empty directory, then auto load as attachments

        var cacheDirectory = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), nameof(Asset), AssetId);
        if (Directory.Exists(cacheDirectory))
        {
            var files = Directory.GetFiles(cacheDirectory);
            foreach (var file in files)
            {
                //Get file ContentType
                var contentType = Path.GetExtension(file).ToLower() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".webp" => "image/webp",
                    ".heic" or ".heif" => "image/heic",
                    ".pdf" => "application/pdf",
                    ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" or ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/octet-stream"
                };

                var bytes = await File.ReadAllBytesAsync(file);
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Best-effort cleanup of staged cache files
                }

                Attachments.Add(new Attachment
                {
                    Id = Guid.Empty, // New attachment; ID will be assigned by server
                    OriginFileName = Path.GetFileName(file),
                    LocalFileName = Path.GetFileName(file),
                    ContentType = contentType,
                    FileSize = bytes.Length,
                    PendingUploadBytes = bytes,
                    AttachmentTypeId = _defaultAttachmentTypeId,
                    AttachmentType = _defaultAttachmentType
                });
            }

            if (files.Length > 0)
            {
                CurrentAttachmentIndex = 0;
                NotifyAttachmentCarouselChanged();
                CurrentAttachmentPreviewSource = null;
                IsLoadingPreview = false;
                if (Attachments.Count > 0)
                {
                    ApplyShareMimeToStagedAttachments();
                    _ = LoadCurrentAttachmentPreviewAsync();
                }
            }
        }

        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(PageTitle));
    }

    /// <summary>
    /// Applies <see cref="ShareMimeType"/> from Android ACTION_SEND so shared images/PDFs match intent MIME
    /// when extension-based guessing is wrong or missing.
    /// </summary>
    private void ApplyShareMimeToStagedAttachments()
    {
        if (Attachments.Count != 1 || string.IsNullOrWhiteSpace(ShareMimeType))
        {
            return;
        }

        var mime = ShareMimeType.Trim();
        if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            Attachments[0].ContentType = mime;
        }
    }

    private async Task LoadAttributesAsync(Guid assetId)
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/attributes/by-parent/Asset/{assetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<AttributeItemDto>>>();
                var attrs = apiResponse?.Data;

                // Ensure collection modifications run on UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Attributes.Clear();
                    if (attrs != null)
                    {
                        foreach (var attr in attrs.Select(EntityMappings.ToEntity))
                        {
                            Attributes.Add(new AttributeViewModel(attr));
                        }
                    }

                    OnPropertyChanged(nameof(HasAttributes));
                });
            }
        }
        catch
        {
            // Silently fail for secondary data
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(HasAttributes)));
        }
    }

    private async Task LoadNotesAsync(Guid assetId)
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/notes/by-parent/Asset/{assetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<NoteDto>>>();
                var notes = apiResponse?.Data;

                // Ensure collection modifications and property notifications run on the UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Notes.Clear();
                    if (notes != null)
                    {
                        foreach (var note in notes.Select(EntityMappings.ToEntity))
                        {
                            Notes.Add(new NoteViewModel(note));
                        }
                    }

                    OnPropertyChanged(nameof(HasNotes));
                });
            }
        }
        catch
        {
            // Silently fail for secondary data
        }
        finally
        {
            // Ensure HasNotes is raised on the UI thread even if the fetch failed
            MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(HasNotes)));
        }
    }

    private async Task LoadAttachmentsAsync(Guid assetId)
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/attachments/by-parent/Asset/{assetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<AttachmentDto>>>();
                Attachments.Clear();
                if (apiResponse?.Data != null)
                {
                    var attachments = apiResponse.Data.Select(EntityMappings.ToEntity).ToList();
                    _attachmentTypeCache.PopulateAll(
                        attachments,
                        a => a.AttachmentTypeId,
                        (a, c) => a.AttachmentType = c is null ? null : EntityMappings.ToEntity(c));

                    foreach (var attachment in attachments)
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
            CurrentAttachmentIndex = 0;
            NotifyAttachmentCarouselChanged();
            CurrentAttachmentPreviewSource = null;
            IsLoadingPreview = false;
            if (Attachments.Count > 0)
            {
                _ = LoadCurrentAttachmentPreviewAsync();
            }
        }
    }

    // ── Save ───────────────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        if (_loadedAsset == null)
        {
            return;
        }

        // Basic validation
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Name is required.", "OK");
            return;
        }

        try
        {
            IsSaving = true;
            (SaveCommand as Command)?.ChangeCanExecute();

            // Apply form values back to the asset
            _loadedAsset.Name = Name.Trim();
            _loadedAsset.PurchasedFrom = string.IsNullOrWhiteSpace(PurchasedFrom) ? null : PurchasedFrom.Trim();
            _loadedAsset.PurchaseDate = PurchaseDate;
            _loadedAsset.CategoryId = SelectedCategory?.Id;
            _loadedAsset.WarrantyTypeId = SelectedWarrantyType?.Id;
            _loadedAsset.WarrantyExpiration = SelectedWarrantyType != null ? WarrantyExpiration : null;
            // Update asset
            HttpResponseMessage assetResponse;
            if (_loadedAsset.Id == Guid.Empty)
            {
                assetResponse = await _apiClient.PostAsJsonAsync("api/assets", EntityMappings.ToCreateRequest(_loadedAsset));
                if (!assetResponse.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Failed to create asset.", "OK");
                    return;
                }

                var created = await assetResponse.Content.ReadFromJsonAsync<ApiResponse<AssetDto>>();
                if (created?.Data != null)
                {
                    _loadedAsset.Id = created.Data.Id;
                }
            }
            else
            {
                assetResponse = await _apiClient.PutAsJsonAsync($"api/assets/{_loadedAsset.Id}", EntityMappings.ToUpdateRequest(_loadedAsset));
                if (!assetResponse.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Failed to save asset.", "OK");
                    return;
                }
            }

            // Save attributes
            await SaveAttributesAsync(_loadedAsset.Id);

            // Save notes
            await SaveNotesAsync(_loadedAsset.Id);

            await SaveAttachmentsAsync(_loadedAsset.Id);

            await GoBackAsync(true);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Error saving asset: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
            (SaveCommand as Command)?.ChangeCanExecute();
        }
    }

    private async Task SaveAttributesAsync(Guid assetId)
    {
        foreach (var vm in Attributes.Where(a => !string.IsNullOrWhiteSpace(a.Key)))
        {
            try
            {
                var attr = vm.Model;
                // Ensure model reflects current vm
                attr.Key = vm.Key;
                attr.Value = vm.Value;

                if (attr.Id == Guid.Empty)
                {
                    // New attribute — POST
                    attr.ParentId = assetId;
                    attr.ParentType = nameof(Asset);
                    var response = await _apiClient.PostAsJsonAsync("api/attributes", EntityMappings.ToCreateRequest(attr));
                    if (response.IsSuccessStatusCode)
                    {
                        var wrapped = await response.Content.ReadFromJsonAsync<ApiResponse<AttributeItemDto>>();
                        if (wrapped?.Success == true && wrapped.Data != null && wrapped.Data.Id != Guid.Empty)
                        {
                            attr.Id = wrapped.Data.Id;
                        }
                    }
                }
                else
                {
                    // Existing attribute — PUT
                    await _apiClient.PutAsJsonAsync($"api/attributes/{attr.Id}", EntityMappings.ToUpdateRequest(attr));
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Error saving attribute: {ex.Message}", "OK");
            }
        }
    }

    private async Task SaveNotesAsync(Guid assetId)
    {
        foreach (var noteVm in Notes.Where(n => !string.IsNullOrWhiteSpace(n.Title) || !string.IsNullOrWhiteSpace(n.Content)))
        {
            try
            {
                Note note = noteVm.Model;
                // Flush view-model text (Entry/Editor may not have pushed to source yet without focus loss).
                note.Title = noteVm.Title;
                note.Content = noteVm.Content;

                if (note.Id == Guid.Empty)
                {
                    // New note — POST
                    note.ParentId = assetId;
                    note.ParentType = nameof(Asset);
                    var response = await _apiClient.PostAsJsonAsync("api/notes", EntityMappings.ToCreateRequest(note));
                    if (response.IsSuccessStatusCode)
                    {
                        var wrapped = await response.Content.ReadFromJsonAsync<ApiResponse<NoteDto>>();
                        if (wrapped?.Success == true && wrapped.Data != null && wrapped.Data.Id != Guid.Empty)
                        {
                            note.Id = wrapped.Data.Id;
                        }
                    }
                }
                else
                {
                    // Existing note — PUT
                    await _apiClient.PutAsJsonAsync($"api/notes/{note.Id}", EntityMappings.ToUpdateRequest(note));
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Error saving note: {ex.Message}", "OK");
            }
        }
    }

    private async Task SaveAttachmentsAsync(Guid assetId)
    {
        foreach (var attachment in Attachments)
        {
            try
            {
                if (attachment.Id == Guid.Empty)
                {
                    // New attachment — POST
                    attachment.ParentId = assetId;
                    attachment.ParentType = nameof(Asset);
                    attachment.AttachmentTypeId = attachment.AttachmentType?.Id;

                    byte[]? fileBytes = attachment.PendingUploadBytes;
                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        //TODO: Validate if still needed
                        var filePath = Path.IsPathRooted(attachment.LocalFileName)
                            ? attachment.LocalFileName
                            : Path.Combine(FileSystem.CacheDirectory, attachment.LocalFileName);

                        if (File.Exists(filePath))
                        {
                            fileBytes = await File.ReadAllBytesAsync(filePath);
                        }
                    }

                    if (fileBytes != null && fileBytes.Length > 0)
                    {
                        await _apiClient.UploadAttachmentAsync(fileBytes, attachment);
                        attachment.PendingUploadBytes = null;

                        var cachePath = Path.IsPathRooted(attachment.LocalFileName)
                            ? attachment.LocalFileName
                            : Path.Combine(FileSystem.CacheDirectory, attachment.LocalFileName);
                        if (File.Exists(cachePath))
                        {
                            File.Delete(cachePath);
                        }
                    }
                }
                else if (attachment.PendingUploadBytes != null)
                {
                    //if edited, flag old for deletion and re-upload
                    byte[]? fileBytes = attachment.PendingUploadBytes;

                    if (fileBytes != null && fileBytes.Length > 0)
                    {
                        var newAttachment = new Attachment(attachment);

                        await _apiClient.UploadAttachmentAsync(fileBytes, newAttachment);
                        await _apiClient.DeleteAsync($"api/attachments/{attachment.Id}");

                        //TODO: Remove cache for old file
                        var localFilePath = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), nameof(Asset), AssetId, attachment.LocalFileName);
                        if (File.Exists(localFilePath))
                        {
                            CurrentAttachmentPreviewSource = ImageSource.FromFile(localFilePath);
                        }
                    }
                }
                else
                {
                    // Existing attachment with no file changes
                    // TODO: optimize to prevent unneeded calls
                    await _apiClient.PutAsJsonAsync($"api/attachments/{attachment.Id}", attachment);
                }
            }
            catch (Exception ex)
            {
                //log to device logs
                Console.WriteLine($"Error saving attachment '{attachment.LocalFileName}': {ex}");
            }
        }
    }

    // ── Remove helpers ─────────────────────────────────────────────────────────

    private async Task RemoveAttributeAsync(AttributeViewModel attrViewModel)
    {
        if (attrViewModel == null)
        {
            return;
        }

        Entities.Attribute attr = attrViewModel.Model;

        if (attr.Id != Guid.Empty)
        {
            try
            {
                await _apiClient.DeleteAsync($"api/attributes/{attr.Id}");
            }
            catch
            {
                // Silently fail — item already removed from UI
            }
        }

        Attributes.Remove(attrViewModel);
        OnPropertyChanged(nameof(HasAttributes));
    }

    private async Task RemoveNoteAsync(NoteViewModel noteViewModel)
    {
        if (noteViewModel == null)
        {
            return;
        }

        Note note = noteViewModel.Model;

        if (note.Id != Guid.Empty)
        {
            try
            {
                await _apiClient.DeleteAsync($"api/notes/{note.Id}");
            }
            catch
            {
                // Silently fail — item already removed from UI
            }
        }

        Notes.Remove(noteViewModel);
        OnPropertyChanged(nameof(HasNotes));
    }
    private async Task RemoveAttachmentAsync()
    {
        try
        {
            if (CurrentAttachment is null)
            {
                return;
            }

            if (CurrentAttachment.Id == Guid.Empty)
            {
                CurrentAttachment.PendingUploadBytes = null;

                var filePath = Path.IsPathRooted(CurrentAttachment.LocalFileName)
                    ? CurrentAttachment.LocalFileName
                    : Path.Combine(FileSystem.CacheDirectory, CurrentAttachment.LocalFileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            else
            {
                try
                {
                    await _apiClient.DeleteAsync($"api/attachments/{CurrentAttachment.Id}");
                }
                catch
                {
                    // Silently fail — item already removed from UI
                }
            }

            Attachments.Remove(CurrentAttachment);
            if (CurrentAttachmentIndex > 0) //only if not first
            {
                CurrentAttachmentIndex--;
            }

            NotifyAttachmentCarouselChanged();
            _ = LoadCurrentAttachmentPreviewAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
            "Failed to remove Attachment",
            $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
            "OK");
        }
    }

    // ── Warranty recalculation ─────────────────────────────────────────────────

    private void RecalculateWarrantyExpiration()
    {
        if (SelectedWarrantyType == null)
        {
            return;
        }

        // Apply Years, Months, Days in order (last defined wins, matching server logic)
        var expiration = PurchaseDate;

        if (SelectedWarrantyType.Years.HasValue)
        {
            expiration = PurchaseDate.AddYears(SelectedWarrantyType.Years.Value);
        }

        if (SelectedWarrantyType.Months.HasValue)
        {
            expiration = PurchaseDate.AddMonths(SelectedWarrantyType.Months.Value);
        }

        if (SelectedWarrantyType.Days.HasValue)
        {
            expiration = PurchaseDate.AddDays(SelectedWarrantyType.Days.Value);
        }

        WarrantyExpiration = expiration;
    }

    // ── Navigation ─────────────────────────────────────────────────────────────

    private async Task GoBackAsync(bool withUpdate = false)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "refresh", withUpdate }
            };

            await Shell.Current.GoToAsync("..", parameters);
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

    //Attachments

    private async Task AddAttachmentPhoto()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo != null)
                {
                    await using Stream sourceStream = await photo.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await sourceStream.CopyToAsync(ms);
                    var bytes = ms.ToArray();

                    Attachments.Add(new Attachment
                    {
                        Id = Guid.Empty, // New attachment; ID will be assigned by server
                        OriginFileName = photo.FileName,
                        LocalFileName = photo.FileName,
                        ContentType = photo.ContentType ?? "image/jpeg",
                        FileSize = bytes.Length,
                        PendingUploadBytes = bytes,
                        AttachmentTypeId = _defaultAttachmentTypeId, // Optionally set a default type
                        AttachmentType = _defaultAttachmentType
                    });

                    CurrentAttachmentIndex = Attachments.Count - 1; // Move carousel to the newly added attachment

                    NotifyAttachmentCarouselChanged();
                    _ = LoadCurrentAttachmentPreviewAsync();
                }
            }
        }
        catch (Exception ex)
        {
            // Surface the full exception so the root cause is visible
            await Shell.Current.DisplayAlertAsync(
                "Photo Error",
                $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "OK");
        }
    }

    private async Task AddAttachmentGallery()
    {
        try
        {

            var results = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
            {
                // Default is 1; set to 0 for no limit
                SelectionLimit = 10,
                // Optional processing for images
                MaximumWidth = 2048,
                MaximumHeight = 2048,
                CompressionQuality = 95,
                RotateImage = true,
                PreserveMetaData = true,
            });

            foreach (var file in results)
            {
                await using Stream sourceStream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await sourceStream.CopyToAsync(ms);
                var bytes = ms.ToArray();

                Attachments.Add(new Attachment
                {
                    Id = Guid.Empty, // New attachment; ID will be assigned by server
                    OriginFileName = file.FileName,
                    LocalFileName = file.FileName,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    FileSize = bytes.Length,
                    PendingUploadBytes = bytes,
                    AttachmentTypeId = _defaultAttachmentTypeId,
                    AttachmentType = _defaultAttachmentType
                });

                CurrentAttachmentIndex = Attachments.Count - 1; // Move carousel to the newly added attachment
            }

            NotifyAttachmentCarouselChanged();
            _ = LoadCurrentAttachmentPreviewAsync();
        }
        catch (Exception ex)
        {
            // Surface the full exception so the root cause is visible
            await Shell.Current.DisplayAlertAsync(
                "Gallery Error",
                $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "OK");
        }
    }

    /// <summary>Fires all property-change notifications related to the carousel state.</summary>
    public void NotifyAttachmentCarouselChanged()
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
    public async Task LoadCurrentAttachmentPreviewAsync()
    {
        CurrentAttachmentPreviewSource = null;

        if (CurrentAttachment == null || !CurrentAttachmentIsImage)
        {
            return;
        }

        try
        {
            IsLoadingPreview = true;

            // Viewer rotate/crop and new uploads use in-memory bytes first.
            var pending = CurrentAttachment.PendingUploadBytes;
            if (pending != null && pending.Length > 0)
            {
                CurrentAttachmentPreviewSource = ImageSource.FromStream(() => new MemoryStream(pending));
                return;
            }

            if (CurrentAttachment.Id == Guid.Empty)
            {
                // Legacy: preview from cache path
                var localFilePath = Path.IsPathRooted(CurrentAttachment.LocalFileName)
                    ? CurrentAttachment.LocalFileName
                    : Path.Combine(FileSystem.CacheDirectory, CurrentAttachment.LocalFileName);

                if (File.Exists(localFilePath))
                {
                    CurrentAttachmentPreviewSource = ImageSource.FromFile(localFilePath);
                }
            }
            else // Existing attachment — load from API
            {
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
        var ct when ct?.StartsWith("image/") == true => "🖼️",
        var ct when ct?.Contains("pdf") == true => "📄",
        var ct when ct?.Contains("word") == true => "📝",
        var ct when ct?.Contains("excel") == true || ct?.Contains("spreadsheet") == true => "📊",
        var ct when ct?.Contains("video") == true => "🎬",
        var ct when ct?.Contains("audio") == true => "🎵",
        var ct when ct?.Contains("zip") == true || ct?.Contains("compressed") == true => "📦",
        _ => "📎"
    };

    private async Task OpenAttachmentAsync(Attachment? attachment)
    {
        if (attachment == null)
        {
            return;
        }

        await SafeExecuteAsync(
            () => _attachmentViewerNavigation.PresentForAssetEditAsync(attachment, editable: true),
            onError: ex => ErrorMessage = $"Could not open attachment: {ex.Message}");
    }
}
