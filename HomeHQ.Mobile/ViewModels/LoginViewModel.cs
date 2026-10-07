using System.Windows.Input;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly SettingsService _settings;

    public string ServerUrl
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string Username
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string Password
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string? ErrorMessage
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool ShowServerUrl
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string LoginButtonText => IsBusy ? "Signing in..." : "Sign In";

    public ICommand LoginCommand { get; }
    public ICommand ToggleServerUrlCommand { get; }

    public LoginViewModel(AuthService authService, SettingsService settings)
    {
        _authService = authService;
        _settings = settings;

        // Load current server URL
        ServerUrl = _settings.ApiBaseUrl;
        ShowServerUrl = !_settings.HasCustomApiUrl; // Show by default if not configured

#if DEBUG
        ServerUrl = "https://192.168.0.151:7176";
        Username = "sysadmin";
        Password = "Password123!";
#endif

        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
        ToggleServerUrlCommand = new Command(() => ShowServerUrl = !ShowServerUrl);
    }

    public async Task<bool> CheckAuthenticationAsync()
    {
        return await SafeExecuteAsync(
            () => _authService.IsAuthenticatedAsync(),
            defaultValue: false,
            onError: ex => ErrorMessage = $"Authentication check failed: {ex.Message}");
    }

    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

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

                // Navigate to Dashboard - AppShell.OnNavigated will handle pending image check
                await Shell.Current.GoToAsync("//Dashboard");
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
}
