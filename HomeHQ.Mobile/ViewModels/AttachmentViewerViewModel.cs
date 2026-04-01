using HomeHQ.Mobile.Services;
using HomeHQ.Entities;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(AttachmentId), "attachmentId")]
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
            }
        }
    }

    public ImageSource? ImageSource
    {
        get;
        set => SetProperty(ref field, value);
    }
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

    public bool ShowContent => !IsLoading && !HasError && Attachment != null;
    public string FileName => Attachment?.OriginFileName ?? "Attachment";

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

    public AttachmentViewerViewModel(ApiClient apiClient, SettingsService settingsService)
    {
        _apiClient = apiClient;
        _settingsService = settingsService;

        GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenExternallyCommand = new Command(async () => await OpenExternallyAsync());
        RefreshCommand = new Command(async () => await LoadAttachmentAsync());
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
        }
    }

    private async Task LoadImageAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync($"api/attachments/{AttachmentId}/data");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }
        catch
        {
            // Image load failed, will show error state
            HasError = true;
            ErrorMessage = "Failed to load image";
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
