using System.Windows.Input;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly SettingsService _settings;
    private readonly SharedImageService _sharedImageService;

    private string _serverUrl = string.Empty;
    public string ServerUrl
    {
        get => _serverUrl;
        set => SetProperty(ref _serverUrl, value);
    }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
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

    private bool _showServerUrl;
    public bool ShowServerUrl
    {
        get => _showServerUrl;
        set => SetProperty(ref _showServerUrl, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string LoginButtonText => IsBusy ? "Signing in..." : "Sign In";

    public ICommand LoginCommand { get; }
    public ICommand ToggleServerUrlCommand { get; }

    public LoginViewModel(AuthService authService, SettingsService settings, SharedImageService sharedImageService)
    {
        _authService = authService;
        _settings = settings;
        _sharedImageService = sharedImageService;

        // Load current server URL
        _serverUrl = _settings.ApiBaseUrl;
        _showServerUrl = !_settings.HasCustomApiUrl; // Show by default if not configured

        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
        ToggleServerUrlCommand = new Command(() => ShowServerUrl = !ShowServerUrl);
    }

    public async Task<bool> CheckAuthenticationAsync()
    {
        return await _authService.IsAuthenticatedAsync();
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return;

        // Validate server URL
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            ErrorMessage = "Please enter the server URL";
            ShowServerUrl = true;
            return;
        }

        if (!Uri.TryCreate(ServerUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            ErrorMessage = "Please enter a valid server URL (e.g., https://example.com)";
            ShowServerUrl = true;
            return;
        }

        // Validate credentials
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Please enter your username";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your password";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            OnPropertyChanged(nameof(LoginButtonText));

            // Save server URL before attempting login
            _settings.SetApiBaseUrl(ServerUrl.Trim());

            var result = await _authService.LoginAsync(Username.Trim(), Password);

            if (result.IsSuccess)
            {
                // Clear sensitive data
                Password = string.Empty;

                // Check for pending shared image
                if (CheckAndLoadPendingImage())
                {
                    await Shell.Current.GoToAsync("//Assets/Create");
                }
                else
                {
                    await Shell.Current.GoToAsync("//Dashboard");
                }
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(LoginButtonText));
        }
    }

    /// <summary>
    /// Checks for pending image in Preferences and loads it into SharedImageService.
    /// Returns true if a pending image was found.
    /// </summary>
    private bool CheckAndLoadPendingImage()
    {
        try
        {
            var imagePath = Preferences.Get("pending_image_path", string.Empty);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                var fileName = Preferences.Get("pending_image_filename", string.Empty);
                var contentType = Preferences.Get("pending_image_contenttype", string.Empty);

                // Load into shared service
                _sharedImageService.SetPendingImage(
                    imagePath,
                    string.IsNullOrEmpty(fileName) ? null : fileName,
                    string.IsNullOrEmpty(contentType) ? null : contentType);

                // Clear preferences
                Preferences.Remove("pending_image_path");
                Preferences.Remove("pending_image_filename");
                Preferences.Remove("pending_image_contenttype");

                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking for pending image: {ex}");
        }

        return false;
    }
}
