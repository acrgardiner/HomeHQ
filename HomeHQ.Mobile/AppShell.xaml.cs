using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService? _authService;
    private bool _setupExpanded;
    private bool _adminExpanded;
    private bool _isAdmin;

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

        // Handle navigation events
        Navigated += OnNavigated;

        ApplySetupExpandedState();
        ApplyAdminExpandedState();
    }

    private void OnSetupMenuItemClicked(object? sender, EventArgs e)
    {
        _setupExpanded = !_setupExpanded;
        ApplySetupExpandedState();

        // Keep the flyout open so the user can pick a Setup page after expanding.
        FlyoutIsPresented = true;
    }

    private void OnAdminMenuItemClicked(object? sender, EventArgs e)
    {
        _adminExpanded = !_adminExpanded;
        ApplyAdminExpandedState();
        FlyoutIsPresented = true;
    }

    private void ApplySetupExpandedState()
    {
        CategoriesFlyoutItem.FlyoutItemIsVisible = _setupExpanded;
        WarrantyTypesFlyoutItem.FlyoutItemIsVisible = _setupExpanded;
        AttachmentTypesFlyoutItem.FlyoutItemIsVisible = _setupExpanded;
        SetupMenuItem.Text = _setupExpanded ? "Setup  ˅" : "Setup  ›";
    }

    private void ApplyAdminExpandedState()
    {
        if (!_isAdmin)
        {
            _adminExpanded = false;
        }

        Shell.SetFlyoutItemIsVisible(AdminMenuItem, _isAdmin);
        AdminDashboardFlyoutItem.FlyoutItemIsVisible = _isAdmin && _adminExpanded;
        UsersFlyoutItem.FlyoutItemIsVisible = _isAdmin && _adminExpanded;
        MaintenanceFlyoutItem.FlyoutItemIsVisible = _isAdmin && _adminExpanded;
        LogsFlyoutItem.FlyoutItemIsVisible = _isAdmin && _adminExpanded;
        AdminMenuItem.Text = _adminExpanded ? "Admin  ˅" : "Admin  ›";
    }

    private async void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var currentRoute = Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
        var isLoginPage = currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase);

        // Update flyout behavior based on current page
        FlyoutBehavior = isLoginPage ? FlyoutBehavior.Disabled : FlyoutBehavior.Flyout;

        if (!isLoginPage)
        {
            await RefreshAdminVisibilityAsync();
            await RefreshFlyoutFooterAsync();
        }
        else
        {
            _isAdmin = false;
            _adminExpanded = false;
            ApplyAdminExpandedState();
        }
    }

    private async Task RefreshAdminVisibilityAsync()
    {
        if (_authService is null)
        {
            return;
        }

        try
        {
            if (await _authService.IsAuthenticatedAsync())
            {
                // Refresh roles if not cached yet (e.g. app restart with stored token)
                var roles = await _authService.GetRolesAsync();
                if (roles.Count == 0)
                {
                    await _authService.RefreshUserInfoAsync();
                }

                _isAdmin = await _authService.IsAdminAsync();
            }
            else
            {
                _isAdmin = false;
                _adminExpanded = false;
            }
        }
        catch
        {
            _isAdmin = false;
        }

        ApplyAdminExpandedState();
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
            FlyoutFooterControl.SetUsername(username);
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
