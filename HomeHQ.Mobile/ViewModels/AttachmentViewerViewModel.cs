using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Mobile.Services;
using MauiNativePdfView.Abstractions;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(Attachment), "attachment")]
[QueryProperty(nameof(Editable), "editable")]
public class AttachmentViewerViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly SettingsService _settingsService;

    /// <summary>When set, rotate/crop updates <see cref="Attachment.PendingUploadBytes"/> on this instance (Asset Edit session).</summary>

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

    public bool IsAdjustMode
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

    public string AdjustModeTitle
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public double AdjustMinimum
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public double AdjustMaximum
    {
        get;
        private set => SetProperty(ref field, value);
    } = 2;

    public double AdjustAmount
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(AdjustAmountLabel));
                _ = PreviewAdjustAsync();
            }
        }
    }

    public string AdjustAmountLabel => $"{AdjustAmount:0.00}";

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

    /// <summary>Primary edit icons (rotate, crop, filters) — hidden while crop/adjust is active.</summary>
    public bool ShowEditToolButtons => ShowImageToolbar && !IsCropMode && !IsAdjustMode;

    /// <summary>Hides "Crop" while already in crop mode.</summary>
    public bool ShowStartCropButton => ShowEditToolButtons;

    /// <summary>Rotate is hidden during crop mode to avoid stacked transforms.</summary>
    public bool ShowRotateButton => ShowEditToolButtons;

    private byte[]? _rawImageBytes;
    private byte[]? _adjustSourceBytes;
    private ImageAdjustKind _adjustKind;
    private CancellationTokenSource? _adjustPreviewCts;
    private bool _suppressAdjustPreview;

    public PdfSource PdfSource
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
    public string FileName => Attachment?.OriginFileName ?? Attachment?.LocalFileName ?? "Attachment";

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
    public ICommand RotateClockwiseCommand { get; }
    public ICommand RotateCounterClockwiseCommand { get; }
    public ICommand StartCropCommand { get; }
    public ICommand ApplyCropCommand { get; }
    public ICommand CancelCropCommand { get; }
    public ICommand ApplyGrayscaleCommand { get; }
    public ICommand StartContrastCommand { get; }
    public ICommand StartSharpnessCommand { get; }
    public ICommand ApplyAdjustCommand { get; }
    public ICommand CancelAdjustCommand { get; }

    public AttachmentViewerViewModel(ApiClient apiClient, SettingsService settingsService)
    {
        _apiClient = apiClient;
        _settingsService = settingsService;

        IsImageEditBusy = false;

        GoBackCommand = new Command(async () => await GoBackAsync());
        OpenExternallyCommand = new Command(async () => await OpenExternallyAsync());
        RotateClockwiseCommand = new Command(async () => await RotateAsync(90), () => ShowEditToolButtons && !IsImageEditBusy);
        RotateCounterClockwiseCommand = new Command(async () => await RotateAsync(-90), () => ShowEditToolButtons && !IsImageEditBusy);
        StartCropCommand = new Command(() => StartCrop(), () => ShowStartCropButton && !IsImageEditBusy);
        ApplyCropCommand = new Command(async () => await ApplyCropAsync(), () => IsCropMode && !IsImageEditBusy);
        CancelCropCommand = new Command(() => CancelCrop(), () => IsCropMode && !IsImageEditBusy);
        ApplyGrayscaleCommand = new Command(async () => await ApplyGrayscaleAsync(), () => ShowEditToolButtons && !IsImageEditBusy);
        StartContrastCommand = new Command(() => StartAdjust(ImageAdjustKind.Contrast), () => ShowEditToolButtons && !IsImageEditBusy);
        StartSharpnessCommand = new Command(() => StartAdjust(ImageAdjustKind.Sharpness), () => ShowEditToolButtons && !IsImageEditBusy);
        ApplyAdjustCommand = new Command(async () => await ApplyAdjustAsync(), () => IsAdjustMode && !IsImageEditBusy);
        CancelAdjustCommand = new Command(() => CancelAdjust(), () => IsAdjustMode && !IsImageEditBusy);
    }

    /// <summary>
    /// Loads the viewer from the same <see cref="Attachment"/> instance used on Asset Edit (modal).
    /// Rotate/crop updates <see cref="Attachment.PendingUploadBytes"/> on that instance.
    /// </summary>
    public async Task InitializeForAssetEditAsync(Attachment linked, bool editable)
    {
        Editable = editable;
        Attachment = linked;

        await LoadLinkedAttachmentContentAsync();
    }

    private static void MergeAttachmentMetadata(Attachment from, Attachment into)
    {
        into.ParentId = from.ParentId;
        into.ParentType = from.ParentType;
        into.LocalFileName = from.LocalFileName;
        into.OriginFileName = from.OriginFileName;
        into.ContentType = from.ContentType;
        into.Extension = from.Extension;
        into.FileSize = from.FileSize;
        into.AttachmentTypeId = from.AttachmentTypeId;
        into.Thumb_LocalFileName = from.Thumb_LocalFileName;
        into.Thumb_ContentType = from.Thumb_ContentType;
        into.Thumb_Extension = from.Thumb_Extension;
        into.Thumb_FileSize = from.Thumb_FileSize;
        if (from.AttachmentType != null)
        {
            into.AttachmentType = from.AttachmentType;
        }
    }

    public async Task LoadLinkedAttachmentContentAsync()
    {
        if (Attachment == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            if (Attachment.Id == Guid.Empty)
            {
                var pending = Attachment.PendingUploadBytes;
                if (pending == null || pending.Length == 0)
                {
                    HasError = true;
                    ErrorMessage = "No file data";
                    return;
                }

                // Shares / staged files may be PDF or other types — only decode bitmaps as images.
                OnPropertyChanged(nameof(IsImage));
                OnPropertyChanged(nameof(IsPdf));
                OnPropertyChanged(nameof(IsOtherFile));

                if (IsImage)
                {
                    await ApplyLoadedImageBytesAsync(pending);
                }
                else
                {
                    NotifyImageToolbarProps();
                }

                return;
            }


            if (IsImage)
            {
                await LoadImageAsync();
            }

            if (IsPdf)
            {
                var cacheDir = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), Attachment.ParentType, Attachment.ParentId.ToString());
                var localFilePath = Path.Combine(cacheDir, Attachment.Id.ToString() + Attachment.Extension);

                if (File.Exists(localFilePath))
                {
                }
                else
                {
                    Directory.CreateDirectory(cacheDir);

                    var pdfResponse = await _apiClient.GetAsync($"api/attachments/{Attachment.Id}/data");
                    if (!pdfResponse.IsSuccessStatusCode)
                    {
                        HasError = true;
                        ErrorMessage = "Failed to download PDF";
                        return;
                    }

                    var bytes = await pdfResponse.Content.ReadAsByteArrayAsync();

                    //Also save to cache for future use
                    await File.WriteAllBytesAsync(localFilePath, bytes);
                }

                // Update the PdfSource to point to the local cached file
                if (localFilePath is not null)
                {
                    PdfSource = PdfSource.FromFile(localFilePath);
                }
            }
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

    private void RefreshEditCommandStates()
    {
        ((Command)RotateClockwiseCommand).ChangeCanExecute();
        ((Command)RotateCounterClockwiseCommand).ChangeCanExecute();
        ((Command)StartCropCommand).ChangeCanExecute();
        ((Command)ApplyCropCommand).ChangeCanExecute();
        ((Command)CancelCropCommand).ChangeCanExecute();
        ((Command)ApplyGrayscaleCommand).ChangeCanExecute();
        ((Command)StartContrastCommand).ChangeCanExecute();
        ((Command)StartSharpnessCommand).ChangeCanExecute();
        ((Command)ApplyAdjustCommand).ChangeCanExecute();
        ((Command)CancelAdjustCommand).ChangeCanExecute();
    }

    private void NotifyImageToolbarProps()
    {
        OnPropertyChanged(nameof(ShowImageToolbar));
        OnPropertyChanged(nameof(PanZoomEnabled));
        OnPropertyChanged(nameof(ShowEditToolButtons));
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
            var rotated = await Task.Run(() => ImageEditor.Rotate(_rawImageBytes, degrees, Attachment.ContentType));
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
            var cropped = await Task.Run(() => ImageEditor.Crop(_rawImageBytes, rect, Attachment.ContentType));
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

    private async Task ApplyGrayscaleAsync()
    {
        if (_rawImageBytes == null || Attachment == null)
        {
            return;
        }

        try
        {
            IsImageEditBusy = true;
            var source = _rawImageBytes;
            var contentType = Attachment.ContentType;
            var result = await Task.Run(() => ImageEditor.Grayscale(source, contentType));
            await ReplaceImageBytesAsync(result);
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

    private void StartAdjust(ImageAdjustKind kind)
    {
        if (_rawImageBytes == null || !IsImage)
        {
            return;
        }

        _adjustKind = kind;
        _adjustSourceBytes = _rawImageBytes;
        _suppressAdjustPreview = true;
        if (kind == ImageAdjustKind.Contrast)
        {
            AdjustModeTitle = "Contrast";
            AdjustMinimum = 0.25;
            AdjustMaximum = 2.5;
            AdjustAmount = 1;
        }
        else
        {
            AdjustModeTitle = "Sharpness";
            AdjustMinimum = 0;
            AdjustMaximum = 2;
            AdjustAmount = 1;
        }

        _suppressAdjustPreview = false;
        IsAdjustMode = true;
        RefreshEditCommandStates();
        _ = PreviewAdjustAsync();
    }

    private void CancelAdjust()
    {
        _adjustPreviewCts?.Cancel();
        var original = _adjustSourceBytes;
        _adjustSourceBytes = null;
        IsAdjustMode = false;
        if (original != null)
        {
            _ = RestorePreviewAsync(original);
        }

        RefreshEditCommandStates();
    }

    private async Task ApplyAdjustAsync()
    {
        if (_adjustSourceBytes == null || Attachment == null)
        {
            return;
        }

        try
        {
            IsImageEditBusy = true;
            _adjustPreviewCts?.Cancel();
            var source = _adjustSourceBytes;
            var contentType = Attachment.ContentType;
            var amount = (float)AdjustAmount;
            var kind = _adjustKind;
            var result = await Task.Run(() => ApplyAdjust(source, kind, amount, contentType));
            await ReplaceImageBytesAsync(result);
            _adjustSourceBytes = null;
            IsAdjustMode = false;
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

    private async Task PreviewAdjustAsync()
    {
        if (_suppressAdjustPreview || !IsAdjustMode || _adjustSourceBytes == null || Attachment == null)
        {
            return;
        }

        _adjustPreviewCts?.Cancel();
        _adjustPreviewCts = new CancellationTokenSource();
        var token = _adjustPreviewCts.Token;
        var source = _adjustSourceBytes;
        var contentType = Attachment.ContentType;
        var amount = (float)AdjustAmount;
        var kind = _adjustKind;

        try
        {
            await Task.Delay(180, token);
            byte[] preview;
            if ((kind == ImageAdjustKind.Contrast && Math.Abs(amount - 1f) < 0.001f) ||
                (kind == ImageAdjustKind.Sharpness && amount <= 0.001f))
            {
                preview = source;
            }
            else
            {
                preview = await Task.Run(() => ApplyAdjust(source, kind, amount, contentType), token);
            }
            token.ThrowIfCancellationRequested();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (token.IsCancellationRequested || !IsAdjustMode || !ReferenceEquals(source, _adjustSourceBytes))
                {
                    return;
                }

                ImageSource = ImageSource.FromStream(() => new MemoryStream(preview));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Edit failed", ex.Message, "OK");
        }
    }

    private async Task RestorePreviewAsync(byte[] bytes)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        });
    }

    private static byte[] ApplyAdjust(byte[] source, ImageAdjustKind kind, float amount, string? contentType) =>
        kind == ImageAdjustKind.Contrast
            ? ImageEditor.Contrast(source, amount, contentType)
            : ImageEditor.Sharpen(source, amount, contentType);

    private async Task ReplaceImageBytesAsync(byte[] newBytes)
    {
        await ApplyLoadedImageBytesAsync(newBytes);

        if (Attachment != null)
        {
            Attachment.PendingUploadBytes = newBytes;
            Attachment.FileSize = newBytes.Length;
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

    private async Task ApplyLoadedImageBytesAsync(byte[] bytes)
    {
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

    private string GetLocalCachePath()
    {
        if (Attachment == null)
        {
            return string.Empty;
        }

        return Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), Attachment.ParentType, Attachment.ParentId.ToString(), Attachment.Id.ToString() + Attachment.Extension);
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
            IsAdjustMode = false;
            _adjustPreviewCts?.Cancel();
            _adjustSourceBytes = null;
            _rawImageBytes = null;
            ImagePixelWidth = 0;
            ImagePixelHeight = 0;

            if (Attachment?.PendingUploadBytes is { Length: > 0 } memoryBytes
                && Attachment.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            {
                await ApplyLoadedImageBytesAsync(memoryBytes);
                return;
            }

            var localFilePath = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), Attachment.ParentType, Attachment.ParentId.ToString(), Attachment.LocalFileName);

            byte[] bytes;
            if (File.Exists(localFilePath))
            {
                bytes = await File.ReadAllBytesAsync(localFilePath);
            }
            else
            {
                var response = await _apiClient.GetAsync($"api/attachments/{Attachment.Id}/data");
                if (!response.IsSuccessStatusCode)
                {
                    HasError = true;
                    ErrorMessage = "Failed to download image";
                    return;
                }

                bytes = await response.Content.ReadAsByteArrayAsync();

                //Also save to cache for future use
                await File.WriteAllBytesAsync(localFilePath, bytes);
            }

            await ApplyLoadedImageBytesAsync(bytes);
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
            if (Shell.Current.Navigation.ModalStack.Count > 0)
            {
                await Shell.Current.Navigation.PopModalAsync();
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
            var response = await _apiClient.GetAsync($"api/attachments/{Attachment.Id}/data");
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

    private enum ImageAdjustKind
    {
        Contrast,
        Sharpness
    }
}
