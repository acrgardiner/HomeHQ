using HomeHQ.DTOs;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private readonly AuthService _authService;
    private readonly CacheService<CategoryDto> _categoryCache;
    private readonly CacheService<AttachmentTypeDto> _attachmentTypeCache;
    private readonly CacheService<WarrantyTypeDto> _warrantyTypeCache;

    public SettingsPage(SettingsService settingsService, AuthService authService, CacheService<CategoryDto> categoryCache, CacheService<AttachmentTypeDto> attachmentTypeCache, CacheService<WarrantyTypeDto> warrantyTypeCache)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _authService = authService;
        _categoryCache = categoryCache;
        _attachmentTypeCache = attachmentTypeCache;
        _warrantyTypeCache = warrantyTypeCache;
    }

    private async void OnClearTempFilesClicked(object? sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlertAsync("Clear Temp Files", "This will delete all temporary files in the app. Continue?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            var cacheDir = FileSystem.CacheDirectory;
            if (!Directory.Exists(cacheDir))
            {
                await DisplayAlertAsync("Temp Files", "Temp files are already empty.", "OK");
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

            await DisplayAlertAsync("Temp Files", $"Temp files cleared ({fileCount} files, {totalSizeBytes / (1024.0 * 1024.0):F2} MB).", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to clear temp files: {ex.Message}", "OK");
        }
    }

    private async void OnClearCacheClicked(object? sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlertAsync("Clear Cache", "This will clear the database cache. Continue?", "Yes", "No");
            if (!confirm)
            {
                return;
            }

            //for each cache service, clear the cache
            _categoryCache.ClearCache();
            _attachmentTypeCache.ClearCache();
            _warrantyTypeCache.ClearCache();

            //Rebuild cache for each service
            await Task.WhenAll(
                _categoryCache.EnsureLoadedAsync(true),
                _attachmentTypeCache.EnsureLoadedAsync(true),
                _warrantyTypeCache.EnsureLoadedAsync(true)
            );

            await DisplayAlertAsync("Cache", $"Cache cleared and reloaded.", "OK");
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
