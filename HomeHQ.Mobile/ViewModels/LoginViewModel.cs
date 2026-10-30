using System.Windows.Input;
using System.Collections.ObjectModel;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private readonly SettingsService _settings;

    public ObservableCollection<Connection> Connections { get; } = [];

    public Connection? SelectedConnection
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                _settings.SelectConnection(value);
            }
        }
    }

    public string NewConnectionName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string NewConnectionUrl
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

    public bool ShowConnections
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowAddConnection
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string LoginButtonText => IsBusy ? "Signing in..." : "Sign In";

    public ICommand LoginCommand { get; }

    public ICommand ToggleShowConnectionsCommand { get; }
    public ICommand ToggleAddConnectionCommand { get; }
    public ICommand AddConnectionCommand { get; }
    public ICommand SelectConnectionCommand { get; }
    public ICommand RemoveConnectionCommand { get; }

    public LoginViewModel(AuthService authService, SettingsService settings)
    {
        _authService = authService;
        _settings = settings;

        Connections = new ObservableCollection<Connection>(_settings.Connections);
        SelectedConnection = _settings.SelectedConnection;

#if DEBUG
        //Set to LocalDev connection
        SelectedConnection = Connections.FirstOrDefault(c => c.Name == "LocalDev");
        Username = "sysadmin";
        Password = "Password123!";
#endif

        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
        ToggleShowConnectionsCommand = new Command(() => ShowConnections = !ShowConnections);
        ToggleAddConnectionCommand = new Command(() => ShowAddConnection = !ShowAddConnection);
        AddConnectionCommand = new Command(AddConnection);
        SelectConnectionCommand = new Command<Connection>(SelectConnection);
        RemoveConnectionCommand = new Command<Connection>(RemoveConnection);
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

        if (SelectedConnection is null)
        {
            ErrorMessage = "Please select a connection";
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

            _settings.SelectConnection(SelectedConnection);

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

    private void AddConnection()
    {
        ErrorMessage = null;
        var name = NewConnectionName.Trim();
        var url = NewConnectionUrl.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Please enter a connection name";
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            ErrorMessage = "Please enter a valid server URL (e.g., https://example.com)";
            return;
        }

        if (Connections.Any(connection => connection.Url.Equals(url.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "A connection with this URL already exists";
            return;
        }

        var connection = _settings.AddConnection(name, url);
        Connections.Add(connection);
        SelectedConnection = connection;
        NewConnectionName = string.Empty;
        NewConnectionUrl = string.Empty;
        ShowAddConnection = false;
    }

    private void RemoveConnection(Connection? connection)
    {
        if (connection is null || connection.Fixed)
        {
            return;
        }

        var wasSelected = SelectedConnection == connection;
        if (_settings.RemoveConnection(connection))
        {
            Connections.Remove(connection);
            if (wasSelected)
            {
                SelectedConnection = _settings.SelectedConnection;
            }
        }
    }

    private void SelectConnection(Connection? connection)
    {
        if (connection is not null)
        {
            SelectedConnection = connection;

            ShowConnections = false;
        }
    }
}
