using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(Kind), "kind")]
[QueryProperty(nameof(EntityId), "id")]
public sealed class SetupEditViewModel : BaseViewModel
{
    public const string KindCategory = "category";
    public const string KindWarrantyType = "warrantytype";
    public const string KindAttachmentType = "attachmenttype";

    private readonly ApiClient _apiClient;
    private readonly CacheService<CategoryDto> _categoryCache;
    private readonly CacheService<WarrantyTypeDto> _warrantyTypeCache;
    private readonly CacheService<AttachmentTypeDto> _attachmentTypeCache;

    private Guid _id = Guid.Empty;
    private bool _isDefault;
    private bool _kindSet;
    private bool _idSet;
    private bool _loadQueued;

    public SetupEditViewModel(
        ApiClient apiClient,
        CacheService<CategoryDto> categoryCache,
        CacheService<WarrantyTypeDto> warrantyTypeCache,
        CacheService<AttachmentTypeDto> attachmentTypeCache)
    {
        _apiClient = apiClient;
        _categoryCache = categoryCache;
        _warrantyTypeCache = warrantyTypeCache;
        _attachmentTypeCache = attachmentTypeCache;

        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        GoBackCommand = new Command(async () => await GoBackAsync());
    }

    public string Kind
    {
        get;
        set
        {
            field = value ?? string.Empty;
            _kindSet = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowIconFields));
            OnPropertyChanged(nameof(ShowDurationFields));
            OnPropertyChanged(nameof(PageTitle));
            QueueLoad();
        }
    } = string.Empty;

    public string EntityId
    {
        get;
        set
        {
            field = value ?? string.Empty;
            _idSet = true;
            _ = Guid.TryParse(field, out _id);
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsNew));
            QueueLoad();
        }
    } = string.Empty;

    public bool IsNew => _id == Guid.Empty;

    public string PageTitle => Kind switch
    {
        KindCategory => IsNew ? "New Category" : "Edit Category",
        KindWarrantyType => IsNew ? "New Warranty Type" : "Edit Warranty Type",
        KindAttachmentType => IsNew ? "New Attachment Type" : "Edit Attachment Type",
        _ => IsNew ? "New" : "Edit"
    };

    public bool ShowIconFields => Kind == KindCategory;
    public bool ShowDurationFields => Kind == KindWarrantyType;

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
        set
        {
            if (SetProperty(ref field, value))
            {
                ((Command)SaveCommand).ChangeCanExecute();
            }
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

    public bool ShowContent => !IsLoading && !HasError;

    public string Name
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string Icon
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string YearsText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string MonthsText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string DaysText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public ICommand SaveCommand { get; }
    public ICommand GoBackCommand { get; }

    private void QueueLoad()
    {
        // Wait until Shell has applied both query properties to avoid loading with a stale id/kind.
        if (_loadQueued || !_kindSet || !_idSet || string.IsNullOrWhiteSpace(Kind))
        {
            return;
        }

        _loadQueued = true;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            if (IsNew)
            {
                Name = string.Empty;
                Icon = string.Empty;
                YearsText = string.Empty;
                MonthsText = string.Empty;
                DaysText = string.Empty;
                _isDefault = false;
                return;
            }

            switch (Kind)
            {
                case KindCategory:
                {
                    await _categoryCache.EnsureLoadedAsync();
                    var dto = _categoryCache.GetById(_id);
                    if (dto == null)
                    {
                        // Force refresh once if cache was stale
                        await _categoryCache.EnsureLoadedAsync(forceRefresh: true);
                        dto = _categoryCache.GetById(_id);
                    }

                    if (dto == null)
                    {
                        HasError = true;
                        ErrorMessage = "Category not found.";
                        return;
                    }

                    Name = dto.Name;
                    Icon = dto.Icon ?? string.Empty;
                    break;
                }
                case KindWarrantyType:
                {
                    await _warrantyTypeCache.EnsureLoadedAsync();
                    var dto = _warrantyTypeCache.GetById(_id);
                    if (dto == null)
                    {
                        await _warrantyTypeCache.EnsureLoadedAsync(forceRefresh: true);
                        dto = _warrantyTypeCache.GetById(_id);
                    }

                    if (dto == null)
                    {
                        HasError = true;
                        ErrorMessage = "Warranty type not found.";
                        return;
                    }

                    Name = dto.Name;
                    YearsText = dto.Years?.ToString() ?? string.Empty;
                    MonthsText = dto.Months?.ToString() ?? string.Empty;
                    DaysText = dto.Days?.ToString() ?? string.Empty;
                    _isDefault = dto.Default;
                    break;
                }
                case KindAttachmentType:
                {
                    await _attachmentTypeCache.EnsureLoadedAsync();
                    var dto = _attachmentTypeCache.GetById(_id);
                    if (dto == null)
                    {
                        await _attachmentTypeCache.EnsureLoadedAsync(forceRefresh: true);
                        dto = _attachmentTypeCache.GetById(_id);
                    }

                    if (dto == null)
                    {
                        HasError = true;
                        ErrorMessage = "Attachment type not found.";
                        return;
                    }

                    Name = dto.Name;
                    _isDefault = dto.Default;
                    break;
                }
                default:
                    HasError = true;
                    ErrorMessage = "Unknown setup entity type.";
                    break;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _loadQueued = false;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Name is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Kind))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Unknown setup entity type.", "OK");
            return;
        }

        try
        {
            IsSaving = true;

            var success = Kind switch
            {
                KindCategory => await SaveCategoryAsync(),
                KindWarrantyType => await SaveWarrantyTypeAsync(),
                KindAttachmentType => await SaveAttachmentTypeAsync(),
                _ => false
            };

            if (!success)
            {
                return;
            }

            await GoBackAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Save failed: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task<bool> SaveCategoryAsync()
    {
        var entity = new Category
        {
            Id = _id,
            Name = Name.Trim(),
            Icon = NullIfWhiteSpace(Icon)
        };

        if (IsNew)
        {
            var response = await _apiClient.PostAsJsonAsync("api/categories", EntityMappings.ToCreateRequest(entity));
            return await FinishSaveAsync(response, _categoryCache, "category");
        }

        var putResponse = await _apiClient.PutAsJsonAsync($"api/categories/{_id}", EntityMappings.ToUpdateRequest(entity));
        return await FinishSaveAsync(putResponse, _categoryCache, "category");
    }

    private async Task<bool> SaveWarrantyTypeAsync()
    {
        var entity = new WarrantyType
        {
            Id = _id,
            Name = Name.Trim(),
            Years = ParseOptionalInt(YearsText),
            Months = ParseOptionalInt(MonthsText),
            Days = ParseOptionalInt(DaysText),
            Default = _isDefault
        };

        if (IsNew)
        {
            var response = await _apiClient.PostAsJsonAsync("api/warrantytypes", EntityMappings.ToCreateRequest(entity));
            return await FinishSaveAsync(response, _warrantyTypeCache, "warranty type");
        }

        var putResponse = await _apiClient.PutAsJsonAsync($"api/warrantytypes/{_id}", EntityMappings.ToUpdateRequest(entity));
        return await FinishSaveAsync(putResponse, _warrantyTypeCache, "warranty type");
    }

    private async Task<bool> SaveAttachmentTypeAsync()
    {
        var entity = new AttachmentType
        {
            Id = _id,
            Name = Name.Trim(),
            Default = _isDefault
        };

        if (IsNew)
        {
            var response = await _apiClient.PostAsJsonAsync("api/attachmenttypes", EntityMappings.ToCreateRequest(entity));
            return await FinishSaveAsync(response, _attachmentTypeCache, "attachment type");
        }

        var putResponse = await _apiClient.PutAsJsonAsync($"api/attachmenttypes/{_id}", EntityMappings.ToUpdateRequest(entity));
        return await FinishSaveAsync(putResponse, _attachmentTypeCache, "attachment type");
    }

    private static async Task<bool> FinishSaveAsync<TDto>(
        HttpResponseMessage response,
        CacheService<TDto> cache,
        string entityLabel)
        where TDto : class, IEntityDto
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await Shell.Current.GoToAsync("//Login");
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to save {entityLabel}.", "OK");
            return false;
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TDto>>();
        if (apiResponse?.Success == false)
        {
            var message = apiResponse.Errors.FirstOrDefault() ?? apiResponse.Message ?? $"Failed to save {entityLabel}.";
            await Shell.Current.DisplayAlertAsync("Error", message, "OK");
            return false;
        }

        cache.ClearCache();
        return true;
    }

    private static async Task GoBackAsync()
    {
        await SafeExecuteAsync(() => Shell.Current.GoToAsync(".."));
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseOptionalInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return int.TryParse(text.Trim(), out var value) ? value : null;
    }
}
