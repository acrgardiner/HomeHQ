using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(AttachmentId), "attachmentId")]
[QueryProperty(nameof(Editable), "editable")]
public class AttachmentViewerViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly SettingsService _settingsService;

    public string AttachmentId
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _ = LoadAttachmentAsync();
            }
        }
    } = string.Empty;

    public bool Editable
    {
        get;
        set;
    } = false;

    public Attachment? Attachment
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(IsImage));
                OnPropertyChanged(nameof(IsPdf));
                OnPropertyChanged(nameof(IsOtherFile));
                OnPropertyChanged(nameof(FileTypeIcon));
                NotifyImageToolbarProps();
            }
        }
    }

    public ImageSource? ImageSource
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Decoded bitmap width for crop coordinate mapping.</summary>
    public int ImagePixelWidth
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Decoded bitmap height for crop coordinate mapping.</summary>
    public int ImagePixelHeight
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsCropMode
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                NotifyImageToolbarProps();
                RefreshEditCommandStates();
            }
        }
    }

    public bool IsImageEditBusy
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RefreshEditCommandStates();
            }
        }
    }

    public double CropRelativeX
    {
        get;
        set => SetProperty(ref field, value);
    }

    public double CropRelativeY
    {
        get;
        set => SetProperty(ref field, value);
    }

    public double CropRelativeWidth
    {
        get;
        set => SetProperty(ref field, value);
    } = 1;

    public double CropRelativeHeight
    {
        get;
        set => SetProperty(ref field, value);
    } = 1;

    /// <summary>Toolbar for rotate/crop when an image preview is showing.</summary>
    public bool ShowImageToolbar => ShowContent && IsImage && Editable;

    /// <summary>Pan/zoom on the viewer is disabled while adjusting the crop rectangle.</summary>
    public bool PanZoomEnabled => !IsCropMode;

    /// <summary>Hides "Crop" while already in crop mode.</summary>
    public bool ShowStartCropButton => ShowImageToolbar && !IsCropMode;

    /// <summary>Rotate is hidden during crop mode to avoid stacked transforms.</summary>
    public bool ShowRotateButton => ShowImageToolbar && !IsCropMode;

    private byte[]? _rawImageBytes;
    public string? PdfUrl
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
            NotifyImageToolbarProps();
        }
    }

    public bool HasError
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(ShowContent));
            NotifyImageToolbarProps();
        }
    }

    public string? ErrorMessage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowContent => !IsLoading && !HasError && Attachment != null;
    public string FileName => Attachment?.LocalFileName ?? "Attachment";

    public bool IsImage => Attachment?.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsPdf => Attachment?.ContentType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsOtherFile => !IsImage && !IsPdf;

    public string FileTypeIcon => Attachment?.ContentType switch
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

    public ICommand GoBackCommand { get; }
    public ICommand OpenExternallyCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RotateClockwiseCommand { get; }
    public ICommand RotateCounterClockwiseCommand { get; }
    public ICommand StartCropCommand { get; }
    public ICommand ApplyCropCommand { get; }
    public ICommand CancelCropCommand { get; }

    public AttachmentViewerViewModel(ApiClient apiClient, SettingsService settingsService)
    {
        _apiClient = apiClient;
        _settingsService = settingsService;

        IsImageEditBusy = false;

        GoBackCommand = new Command(async () => await GoBackAsync());
        OpenExternallyCommand = new Command(async () => await OpenExternallyAsync());
        RefreshCommand = new Command(async () => await LoadAttachmentAsync());
        RotateClockwiseCommand = new Command(async () => await RotateAsync(90), () => ShowImageToolbar && !IsCropMode && !IsImageEditBusy);
        RotateCounterClockwiseCommand = new Command(async () => await RotateAsync(-90), () => ShowImageToolbar && !IsCropMode && !IsImageEditBusy);
        StartCropCommand = new Command(() => StartCrop(), () => ShowStartCropButton && !IsImageEditBusy);
        ApplyCropCommand = new Command(async () => await ApplyCropAsync(), () => IsCropMode && !IsImageEditBusy);
        CancelCropCommand = new Command(() => CancelCrop(), () => IsCropMode && !IsImageEditBusy);
    }

    private void RefreshEditCommandStates()
    {
        ((Command)RotateClockwiseCommand).ChangeCanExecute();
        ((Command)RotateCounterClockwiseCommand).ChangeCanExecute();
        ((Command)StartCropCommand).ChangeCanExecute();
        ((Command)ApplyCropCommand).ChangeCanExecute();
        ((Command)CancelCropCommand).ChangeCanExecute();
    }

    private void NotifyImageToolbarProps()
    {
        OnPropertyChanged(nameof(ShowImageToolbar));
        OnPropertyChanged(nameof(PanZoomEnabled));
        OnPropertyChanged(nameof(ShowStartCropButton));
        OnPropertyChanged(nameof(ShowRotateButton));
    }

    private void StartCrop()
    {
        if (_rawImageBytes == null || !IsImage)
        {
            return;
        }

        CropRelativeX = 0;
        CropRelativeY = 0;
        CropRelativeWidth = 1;
        CropRelativeHeight = 1;
        IsCropMode = true;
        RefreshEditCommandStates();
    }

    private void CancelCrop()
    {
        IsCropMode = false;
        RefreshEditCommandStates();
    }

    private async Task RotateAsync(int degrees)
    {
        _ = ShowImageToolbar;
        _ = !IsCropMode;
        _ = !IsImageEditBusy;
        _ = ShowStartCropButton;

        if (_rawImageBytes == null || Attachment == null)
        {
            return;
        }

        try
        {
            IsImageEditBusy = true;
            var rotated = ImageEditor.Rotate(_rawImageBytes, degrees, Attachment.ContentType);
            await ReplaceImageBytesAsync(rotated);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Edit failed", ex.Message, "OK");
        }
        finally
        {
            IsImageEditBusy = false;
        }
    }

    private async Task ApplyCropAsync()
    {
        if (_rawImageBytes == null || Attachment == null || ImagePixelWidth <= 0 || ImagePixelHeight <= 0)
        {
            return;
        }

        try
        {
            IsImageEditBusy = true;

            var w = ImagePixelWidth;
            var h = ImagePixelHeight;
            var x = Math.Clamp((int)Math.Floor(CropRelativeX * w), 0, Math.Max(0, w - 1));
            var y = Math.Clamp((int)Math.Floor(CropRelativeY * h), 0, Math.Max(0, h - 1));
            var right = Math.Clamp((int)Math.Ceiling((CropRelativeX + CropRelativeWidth) * w), x + 1, w);
            var bottom = Math.Clamp((int)Math.Ceiling((CropRelativeY + CropRelativeHeight) * h), y + 1, h);
            var cw = right - x;
            var ch = bottom - y;

            var rect = new ImageCropRectangle(x, y, cw, ch);
            var cropped = ImageEditor.Crop(_rawImageBytes, rect, Attachment.ContentType);
            await ReplaceImageBytesAsync(cropped);
            IsCropMode = false;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Crop failed", ex.Message, "OK");
        }
        finally
        {
            IsImageEditBusy = false;
        }
    }

    private async Task ReplaceImageBytesAsync(byte[] newBytes)
    {
        _rawImageBytes = newBytes;
        var dims = ImageEditor.GetDimensions(newBytes);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ImagePixelWidth = dims.Width;
            ImagePixelHeight = dims.Height;
            ImageSource = ImageSource.FromStream(() => new MemoryStream(newBytes));
        });

        if (Attachment != null)
        {
            var path = GetLocalCachePath();
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await File.WriteAllBytesAsync(path, newBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write edited image to cache: {ex.Message}");
                // Preview still updated in memory; cache write is best-effort.
            }
        }
    }

    private string GetLocalCachePath()
    {
        if (Attachment == null)
        {
            return string.Empty;
        }

        return Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), Attachment.ParentType, Attachment.ParentId.ToString(), Attachment.LocalFileName);
    }

    public async Task LoadAttachmentAsync()
    {
        if (string.IsNullOrEmpty(AttachmentId))
        {
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            // First, get the attachment metadata
            var response = await _apiClient.GetAsync($"api/attachments/{AttachmentId}");
            if (!response.IsSuccessStatusCode)
            {
                HasError = true;
                ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Please log in again"
                    : $"Failed to load attachment: {response.StatusCode}";
                return;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Attachment>>();
            if (apiResponse?.Data == null)
            {
                HasError = true;
                ErrorMessage = "Attachment not found";
                return;
            }

            Attachment = apiResponse.Data;

            // Load the actual file data based on type
            if (IsImage)
            {
                await LoadImageAsync();
            }
            // For PDF and other files, we'll handle them when user wants to view
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
            RefreshEditCommandStates();
        }
    }

    private async Task LoadImageAsync()
    {
        if (Attachment == null)
        {
            return;
        }

        try
        {
            IsCropMode = false;
            _rawImageBytes = null;
            ImagePixelWidth = 0;
            ImagePixelHeight = 0;

            var localFilePath = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), Attachment.ParentType, Attachment.ParentId.ToString(), Attachment.LocalFileName);

            byte[] bytes;
            if (File.Exists(localFilePath))
            {
                bytes = await File.ReadAllBytesAsync(localFilePath);
            }
            else
            {
                var response = await _apiClient.GetAsync($"api/attachments/{AttachmentId}/data");
                if (!response.IsSuccessStatusCode)
                {
                    HasError = true;
                    ErrorMessage = "Failed to download image";
                    return;
                }

                bytes = await response.Content.ReadAsByteArrayAsync();
            }

            _rawImageBytes = bytes;
            var dims = ImageEditor.GetDimensions(bytes);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ImagePixelWidth = dims.Width;
                ImagePixelHeight = dims.Height;
                ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
            });

            RefreshEditCommandStates();
            NotifyImageToolbarProps();
        }
        catch (Exception)
        {
            HasError = true;
            ErrorMessage = "Failed to load image";
        }
    }

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

    private async Task OpenExternallyAsync()
    {
        if (Attachment == null)
        {
            return;
        }

        try
        {
            IsLoading = true;

            // Download the file
            var response = await _apiClient.GetAsync($"api/attachments/{AttachmentId}/data");
            if (!response.IsSuccessStatusCode)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Failed to download file", "OK");
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Save to cache directory
            var fileName = Attachment.OriginFileName;
            var cacheDir = FileSystem.CacheDirectory;
            var filePath = Path.Combine(cacheDir, fileName);

            await File.WriteAllBytesAsync(filePath, bytes);

            // Open with default app
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to open file: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
