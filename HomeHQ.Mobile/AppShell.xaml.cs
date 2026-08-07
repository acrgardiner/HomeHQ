using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService? _authService;
    private bool _setupExpanded;

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

        // Try to get services
        _authService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();

        // Handle navigation events
        Navigated += OnNavigated;

        ApplySetupExpandedState();
    }

    private void OnSetupMenuItemClicked(object? sender, EventArgs e)
    {
        _setupExpanded = !_setupExpanded;
        ApplySetupExpandedState();

        // Keep the flyout open so the user can pick a Setup page after expanding.
        FlyoutIsPresented = true;
    }

    private void ApplySetupExpandedState()
    {
        CategoriesFlyoutItem.FlyoutItemIsVisible = _setupExpanded;
        WarrantyTypesFlyoutItem.FlyoutItemIsVisible = _setupExpanded;
        AttachmentTypesFlyoutItem.FlyoutItemIsVisible = _setupExpanded;
        SetupMenuItem.Text = _setupExpanded ? "Setup  ˅" : "Setup  ›";
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
