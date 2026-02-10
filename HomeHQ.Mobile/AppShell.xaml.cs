using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService? _authService;
    private readonly SharedImageService? _sharedImageService;
    private bool _isCheckingPendingImage;

    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("Login", typeof(LoginPage));
        Routing.RegisterRoute("Dashboard", typeof(DashboardPage));
        Routing.RegisterRoute("Assets", typeof(AssetsPage));
        Routing.RegisterRoute("Settings", typeof(SettingsPage));
        Routing.RegisterRoute("Create", typeof(AssetCreatePage));

        // Try to get services
        _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();
        _sharedImageService = Application.Current?.Handler?.MauiContext?.Services.GetService<SharedImageService>();

        // Handle navigation events
        Navigated += OnNavigated;
    }

    private async void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var currentRoute = Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
        var isLoginPage = currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase);
        var isCreatePage = currentRoute.Contains("Create", StringComparison.OrdinalIgnoreCase);

        // Update flyout behavior based on current page
        FlyoutBehavior = isLoginPage ? FlyoutBehavior.Disabled : FlyoutBehavior.Flyout;

        // Check for pending shared image after navigating to an authenticated page
        // Skip if we're on Login, Create page, or already checking
        if (!isLoginPage && !isCreatePage && !_isCheckingPendingImage)
        {
            await CheckAndNavigateToPendingImageAsync();
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
            }
        }
    }

    /// <summary>
    /// Centralized check for pending shared images.
    /// Called after navigation to any authenticated page.
    /// </summary>
    private async Task CheckAndNavigateToPendingImageAsync()
    {
        if (_sharedImageService == null) return;

        try
        {
            _isCheckingPendingImage = true;

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

                // Navigate to AssetCreatePage
                await GoToAsync("//Assets/Create");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking for pending image: {ex}");
        }
        finally
        {
            _isCheckingPendingImage = false;
        }
    }
}
