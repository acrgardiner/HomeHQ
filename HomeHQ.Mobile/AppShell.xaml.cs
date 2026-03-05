using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService? _authService;

    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("Login", typeof(LoginPage));
        Routing.RegisterRoute("Dashboard", typeof(DashboardPage));
        Routing.RegisterRoute("Assets", typeof(AssetsPage));
        Routing.RegisterRoute("Settings", typeof(SettingsPage));

        // Try to get services
        _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();

        // Handle navigation events
        Navigated += OnNavigated;
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var currentRoute = Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
        var isLoginPage = currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase);

        // Update flyout behavior based on current page
        FlyoutBehavior = isLoginPage ? FlyoutBehavior.Disabled : FlyoutBehavior.Flyout;
    }

    protected override async void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // Skip check for login page
        var targetRoute = args.Target?.Location?.OriginalString ?? string.Empty;
        if (targetRoute.Contains("Login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Check authentication for protected routes
        if (_authService != null)
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                args.Cancel();
                await GoToAsync("//Login");
            }
        }
    }
}
