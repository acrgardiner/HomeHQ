using HomeHQ.Mobile.Services;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(AssetId), "assetId")]
public class AssetEditViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly CacheService<Category> _categoryCache;
    private readonly CacheService<WarrantyType> _warrantyTypeCache;

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

    public ObservableCollection<AttributeValue> Attributes { get; } = [];
    public ObservableCollection<Note> Notes { get; } = [];

    public bool HasAttributes => Attributes.Count > 0;
    public bool HasNotes => Notes.Count > 0;

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand GoBackCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddAttributeCommand { get; }
    public ICommand RemoveAttributeCommand { get; }
    public ICommand AddNoteCommand { get; }
    public ICommand RemoveNoteCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────────

    public AssetEditViewModel(
        ApiClient apiClient,
        CacheService<Category> categoryCache,
        CacheService<WarrantyType> warrantyTypeCache)
    {
        _apiClient = apiClient;
        _categoryCache = categoryCache;
        _warrantyTypeCache = warrantyTypeCache;

        GoBackCommand = new Command(async () => await GoBackAsync());
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        AddAttributeCommand = new Command(() =>
        {
            Attributes.Add(new AttributeValue());
            OnPropertyChanged(nameof(HasAttributes));
        });
        RemoveAttributeCommand = new Command<AttributeValue>(async attr => await RemoveAttributeAsync(attr));
        AddNoteCommand = new Command(() =>
        {
            Notes.Add(new Note());
            OnPropertyChanged(nameof(HasNotes));
        });
        RemoveNoteCommand = new Command<Note>(async note => await RemoveNoteAsync(note));
    }

    // ── Load ───────────────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(AssetId) || !Guid.TryParse(AssetId, out var assetGuid))
        {
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            // Warm reference caches in parallel
            await Task.WhenAll(
                _categoryCache.EnsureLoadedAsync(),
                _warrantyTypeCache.EnsureLoadedAsync());

            // Load asset
            var response = await _apiClient.GetAsync($"api/assets/{AssetId}");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Asset>>();
                if (apiResponse?.Data != null)
                {
                    _loadedAsset = apiResponse.Data;
                    PopulateFormFromAsset(_loadedAsset);
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

            // Load attributes and notes in parallel
            await Task.WhenAll(
                LoadAttributesAsync(assetGuid),
                LoadNotesAsync(assetGuid));
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

    private void PopulateFormFromAsset(Asset asset)
    {
        Name = asset.Name ?? string.Empty;
        PurchasedFrom = asset.PurchasedFrom ?? string.Empty;
        PurchaseDate = asset.PurchaseDate ?? DateTime.Today;

        // Populate picker collections from cache
        Categories.Clear();
        foreach (var cat in _categoryCache.GetAllAsync().GetAwaiter().GetResult())
        {
            Categories.Add(cat);
        }

        WarrantyTypes.Clear();
        foreach (var wt in _warrantyTypeCache.GetAllAsync().GetAwaiter().GetResult().OrderBy(w => w.SortOrder))
        {
            WarrantyTypes.Add(wt);
        }

        // Resolve selected items
        SelectedCategory = asset.CategoryId.HasValue
            ? Categories.FirstOrDefault(c => c.Id == asset.CategoryId.Value)
            : null;

        SelectedWarrantyType = asset.WarrantyTypeId.HasValue
            ? WarrantyTypes.FirstOrDefault(w => w.Id == asset.WarrantyTypeId.Value)
            : null;

        WarrantyExpiration = asset.WarrantyExpiration ?? DateTime.Today;
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
        finally
        {
            OnPropertyChanged(nameof(HasAttributes));
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
        finally
        {
            OnPropertyChanged(nameof(HasNotes));
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
            var assetResponse = await _apiClient.PutAsJsonAsync($"api/assets/{_loadedAsset.Id}", _loadedAsset);
            if (!assetResponse.IsSuccessStatusCode)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Failed to save asset.", "OK");
                return;
            }

            // Save attributes
            await SaveAttributesAsync(_loadedAsset.Id);

            // Save notes
            await SaveNotesAsync(_loadedAsset.Id);

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
        foreach (var attr in Attributes.Where(a => !string.IsNullOrWhiteSpace(a.Attribute)))
        {
            try
            {
                if (attr.Id == Guid.Empty)
                {
                    // New attribute — POST
                    attr.ParentId = assetId;
                    attr.ParentType = nameof(Asset);
                    await _apiClient.PostAsJsonAsync("api/attributevalues", attr);
                }
                else
                {
                    // Existing attribute — PUT
                    await _apiClient.PutAsJsonAsync($"api/attributevalues/{attr.Id}", attr);
                }
            }
            catch
            {
                // Continue saving remaining items
            }
        }
    }

    private async Task SaveNotesAsync(Guid assetId)
    {
        foreach (var note in Notes.Where(n => !string.IsNullOrWhiteSpace(n.Title) || !string.IsNullOrWhiteSpace(n.Content)))
        {
            try
            {
                if (note.Id == Guid.Empty)
                {
                    // New note — POST
                    note.ParentId = assetId;
                    note.ParentType = nameof(Asset);
                    await _apiClient.PostAsJsonAsync("api/notes", note);
                }
                else
                {
                    // Existing note — PUT
                    await _apiClient.PutAsJsonAsync($"api/notes/{note.Id}", note);
                }
            }
            catch
            {
                // Continue saving remaining items
            }
        }
    }

    // ── Remove helpers ─────────────────────────────────────────────────────────

    private async Task RemoveAttributeAsync(AttributeValue attr)
    {
        if (attr == null)
        {
            return;
        }

        Attributes.Remove(attr);
        OnPropertyChanged(nameof(HasAttributes));

        if (attr.Id != Guid.Empty)
        {
            try
            {
                await _apiClient.DeleteAsync($"api/attributevalues/{attr.Id}");
            }
            catch
            {
                // Silently fail — item already removed from UI
            }
        }
    }

    private async Task RemoveNoteAsync(Note note)
    {
        if (note == null)
        {
            return;
        }

        Notes.Remove(note);
        OnPropertyChanged(nameof(HasNotes));

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
            if (withUpdate)
            {
                await Shell.Current.Navigation.PopAsync();
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
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
}
