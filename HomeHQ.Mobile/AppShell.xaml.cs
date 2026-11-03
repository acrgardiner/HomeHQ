using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Pages.Admin;
using HomeHQ.Mobile.Pages.Setup;
using HomeHQ.Mobile.Services;
using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService? _authService;
    private readonly FlyoutViewModel? _flyoutViewModel;

    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        // Register routes for detail/modal pages that are NOT declared in AppShell.xaml.
        // NOTE: Do NOT re-register routes that already have Route="..." on a ShellContent in
        //       the XAML (Login, Dashboard, Assets, Settings) – doing so creates duplicate
        //       registrations which cause "Ambiguous routes matched" exceptions during GoToAsync("..").
        Routing.RegisterRoute("AssetDetail", typeof(AssetDetailPage));
        Routing.RegisterRoute("AssetEdit", typeof(AssetEditPage));
        Routing.RegisterRoute("AttachmentViewer", typeof(AttachmentViewerPage));
        Routing.RegisterRoute("SetupEdit", typeof(SetupEditPage));
        Routing.RegisterRoute("UserEdit", typeof(UserEditPage));

        // Try to get services
        _authService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();
        _flyoutViewModel = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<FlyoutViewModel>();
        CustomFlyoutControl.BindingContext = _flyoutViewModel;

        // Handle navigation events
        Navigated += OnNavigated;

    }

    private async void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var currentRoute = Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
        var isLoginPage = currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase);

        // Update flyout behavior based on current page
        FlyoutBehavior = isLoginPage ? FlyoutBehavior.Disabled : FlyoutBehavior.Flyout;

        if (!isLoginPage)
        {
            if (_flyoutViewModel is not null)
            {
                await _flyoutViewModel.RefreshAdminVisibilityAsync();
            }
            await RefreshFlyoutFooterAsync();
        }
        else
        {
            _flyoutViewModel?.Reset();
        }
    }

    private async Task RefreshFlyoutFooterAsync()
    {
        if (_authService is null)
        {
            return;
        }

        try
        {
            var username = await _authService.GetUsernameAsync();
            CustomFlyoutControl.SetUsername(username);
        }
        catch
        {
            // ignore
        }
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
                return;
            }

            // Gate Admin routes to Admin role
            if (IsAdminRoute(targetRoute) && !await _authService.IsAdminAsync())
            {
                args.Cancel();
                await DisplayAlertAsync("Access Denied", "You do not have permission to access Admin pages.", "OK");
            }
        }
    }

    private static bool IsAdminRoute(string route) =>
        route.Contains("AdminDashboard", StringComparison.OrdinalIgnoreCase)
        || route.Contains("UserEdit", StringComparison.OrdinalIgnoreCase)
        || route.Contains("Maintenance", StringComparison.OrdinalIgnoreCase)
        || route.Contains("//Users", StringComparison.OrdinalIgnoreCase)
        || route.EndsWith("/Users", StringComparison.OrdinalIgnoreCase)
        || route.Equals("Users", StringComparison.OrdinalIgnoreCase)
        || route.Contains("//Logs", StringComparison.OrdinalIgnoreCase)
        || route.EndsWith("/Logs", StringComparison.OrdinalIgnoreCase)
        || route.Equals("Logs", StringComparison.OrdinalIgnoreCase);
}
