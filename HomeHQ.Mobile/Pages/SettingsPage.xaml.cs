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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadSettings();
    }

    private async Task LoadSettings()
    {
        ServerUrlEntry.Text = _settingsService.ApiBaseUrl;

        // Get username from AuthService
        var username = await _authService.GetUsernameAsync();
        UsernameLabel.Text = username ?? "Unknown User";

        VersionLabel.Text = AppInfo.Current.VersionString;
    }

    private async void OnSaveServerClicked(object? sender, EventArgs e)
    {
        var url = ServerUrlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            await DisplayAlert("Error", "Please enter a valid server URL", "OK");
            return;
        }

        _settingsService.SetApiBaseUrl(url);
        await DisplayAlert("Success", "Server URL has been saved", "OK");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Sign Out", "Are you sure you want to sign out?", "Yes", "No");
        if (confirm)
        {
            _authService.Logout();
            await Shell.Current.GoToAsync("//Login");
        }
    }
}
