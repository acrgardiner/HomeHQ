using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private readonly AuthService _authService;

    public SettingsPage(SettingsService settingsService, AuthService authService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _authService = authService;
    }

    private async void OnClearCacheClicked(object? sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlertAsync("Clear Cache", "This will delete all files in the app cache. Continue?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            var cacheDir = FileSystem.CacheDirectory;
            if (!Directory.Exists(cacheDir))
            {
                await DisplayAlertAsync("Cache", "Cache is already empty.", "OK");
                return;
            }

            //var files = Directory.GetFiles(cacheDir);
            var files = new DirectoryInfo(cacheDir).EnumerateFiles("*", SearchOption.AllDirectories).ToList();
            int fileCount = files.Count;
            long totalSizeBytes = 0;

            foreach (var f in files)
            {
                try
                {
                    totalSizeBytes += f.Length;
                    f.Delete();
                } catch {
                    /* ignore individual failures */
                }
            }

            await DisplayAlertAsync("Cache", $"Cache cleared ({fileCount} files, {totalSizeBytes / (1024.0 * 1024.0):F2} MB).", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to clear cache: {ex.Message}", "OK");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadSettings();
    }

    private async Task LoadSettings()
    {
        try
        {
            ServerUrlEntry.Text = _settingsService.ApiBaseUrl;

            // Get username from AuthService
            var username = await _authService.GetUsernameAsync();
            UsernameLabel.Text = username ?? "Unknown User";

            VersionLabel.Text = AppInfo.Current.VersionString;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load settings: {ex.Message}", "OK");
        }
    }

    private async void OnSaveServerClicked(object? sender, EventArgs e)
    {
        try
        {
            var url = ServerUrlEntry.Text?.Trim();
            if (string.IsNullOrEmpty(url))
            {
                await DisplayAlertAsync("Error", "Please enter a valid server URL", "OK");
                return;
            }

            _settingsService.SetApiBaseUrl(url);
            await DisplayAlertAsync("Success", "Server URL has been saved", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save server URL: {ex.Message}", "OK");
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlertAsync("Sign Out", "Are you sure you want to sign out?", "Yes", "No");
            if (confirm)
            {
                _authService.Logout();
                await Shell.Current.GoToAsync("//Login");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Sign out failed: {ex.Message}", "OK");
        }
    }
}
