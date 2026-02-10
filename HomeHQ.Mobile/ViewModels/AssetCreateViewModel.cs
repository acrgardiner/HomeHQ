using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Mobile.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows.Input;

namespace HomeHQ.Mobile.ViewModels;

public class AssetCreateViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly CategoryService _categoryService;
    private readonly SharedImageService _sharedImageService;

    public AssetCreateViewModel(
        ApiClient apiClient,
        AuthService authService,
        CategoryService categoryService,
        SharedImageService sharedImageService)
    {
        _apiClient = apiClient;
        _authService = authService;
        _categoryService = categoryService;
        _sharedImageService = sharedImageService;

        SaveCommand = new Command(async () => await SaveAssetAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(Name));
        CancelCommand = new Command(async () => await CancelAsync());
        RemoveImageCommand = new Command(RemoveImage);
        AddImageCommand = new Command(async () => await PickImageAsync());
    }

    #region Properties

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }
    }

    private string? _purchasedFrom;
    public string? PurchasedFrom
    {
        get => _purchasedFrom;
        set => SetProperty(ref _purchasedFrom, value);
    }

    private DateTime _purchaseDate = DateTime.Today;
    public DateTime PurchaseDate
    {
        get => _purchaseDate;
        set => SetProperty(ref _purchaseDate, value);
    }

    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetProperty(ref _isLoading, value);
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool ShowContent => !IsLoading;

    // Image attachment
    private string? _pendingImagePath;
    public string? PendingImagePath
    {
        get => _pendingImagePath;
        set
        {
            SetProperty(ref _pendingImagePath, value);
            OnPropertyChanged(nameof(HasPendingImage));
            OnPropertyChanged(nameof(PendingImageSource));
        }
    }

    private string? _pendingImageFileName;
    public string? PendingImageFileName
    {
        get => _pendingImageFileName;
        set => SetProperty(ref _pendingImageFileName, value);
    }

    private string? _pendingImageContentType;
    public string? PendingImageContentType
    {
        get => _pendingImageContentType;
        set => SetProperty(ref _pendingImageContentType, value);
    }

    public bool HasPendingImage => !string.IsNullOrEmpty(PendingImagePath) && File.Exists(PendingImagePath);

    public ImageSource? PendingImageSource
    {
        get
        {
            if (HasPendingImage)
            {
                return ImageSource.FromFile(PendingImagePath);
            }
            return null;
        }
    }

    public ObservableCollection<Category> Categories { get; } = [];

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RemoveImageCommand { get; }
    public ICommand AddImageCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Initialize the view model - load categories and check for shared images.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Load categories
            var categories = await _categoryService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            // Check for shared image
            if (_sharedImageService.HasPendingImage)
            {
                PendingImagePath = _sharedImageService.PendingImagePath;
                PendingImageFileName = _sharedImageService.PendingImageFileName;
                PendingImageContentType = _sharedImageService.PendingImageContentType;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAssetAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(Name))
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // Create the asset
            var asset = new Asset
            {
                Name = Name.Trim(),
                PurchasedFrom = PurchasedFrom?.Trim(),
                PurchaseDate = PurchaseDate,
                CategoryId = SelectedCategory?.Id
            };

            var response = await _apiClient.PostAsJsonAsync("api/assets", asset);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await Shell.Current.GoToAsync("//Login");
                    return;
                }
                ErrorMessage = $"Failed to create asset: {response.StatusCode}";
                return;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Asset>>();
            if (apiResponse?.Data == null)
            {
                ErrorMessage = "Failed to create asset: Invalid response";
                return;
            }

            var createdAsset = apiResponse.Data;

            // Upload attachment if we have a pending image
            if (HasPendingImage)
            {
                await UploadAttachmentAsync(createdAsset.Id);
            }

            // Clear the shared image service
            _sharedImageService.ClearPendingImage();

            // Navigate back to assets list
            await Shell.Current.GoToAsync("//Assets");
            await Shell.Current.DisplayAlertAsync("Success", $"Asset '{Name}' created successfully!", "OK");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving asset: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UploadAttachmentAsync(Guid assetId)
    {
        try
        {
            if (!HasPendingImage) return;

            var fileBytes = File.ReadAllBytes(PendingImagePath!);
            var extension = Path.GetExtension(PendingImageFileName ?? PendingImagePath);

            // Create attachment metadata
            var attachment = new Attachment
            {
                ParentId = assetId,
                ParentType = "Asset",
                OriginFileName = PendingImageFileName ?? "attachment" + extension,
                ContentType = PendingImageContentType ?? "application/octet-stream",
                Extension = extension,
                FileSize = fileBytes.Length
            };

            // Upload using multipart form data
            using var content = new MultipartFormDataContent();

            // Add the file
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType);
            content.Add(fileContent, "file", attachment.OriginFileName);

            // Add metadata as form fields
            content.Add(new StringContent(assetId.ToString()), "parentId");
            content.Add(new StringContent("Asset"), "parentType");
            content.Add(new StringContent(attachment.OriginFileName), "originFileName");
            content.Add(new StringContent(attachment.ContentType), "contentType");
            content.Add(new StringContent(attachment.Extension), "extension");

            var response = await _apiClient.PostMultipartAsync("api/attachments/upload", content);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to upload attachment: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error uploading attachment: {ex}");
            // Don't fail the whole operation if attachment upload fails
        }
    }

    private async Task CancelAsync()
    {
        // Clear pending image without saving
        _sharedImageService.ClearPendingImage();
        ClearForm();
        await Shell.Current.GoToAsync("..");
    }

    private void RemoveImage()
    {
        // Delete the local cached file
        if (!string.IsNullOrEmpty(PendingImagePath) && File.Exists(PendingImagePath))
        {
            try
            {
                File.Delete(PendingImagePath);
            }
            catch { /* Ignore */ }
        }

        PendingImagePath = null;
        PendingImageFileName = null;
        PendingImageContentType = null;
        _sharedImageService.ClearPendingImage();
    }

    private async Task PickImageAsync()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select an image"
            });

            if (result != null)
            {
                // Copy to cache directory to ensure we have access
                var cacheFile = Path.Combine(FileSystem.CacheDirectory, result.FileName);

                using var sourceStream = await result.OpenReadAsync();
                using var destStream = File.Create(cacheFile);
                await sourceStream.CopyToAsync(destStream);

                PendingImagePath = cacheFile;
                PendingImageFileName = result.FileName;
                PendingImageContentType = result.ContentType;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error picking image: {ex.Message}";
        }
    }

    private void ClearForm()
    {
        Name = string.Empty;
        PurchasedFrom = null;
        PurchaseDate = DateTime.Today;
        SelectedCategory = null;
        PendingImagePath = null;
        PendingImageFileName = null;
        PendingImageContentType = null;
        ErrorMessage = null;
    }

    #endregion
}
