using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.Controls;

public partial class FlyoutFooter : ContentView
{
    public FlyoutFooter()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    public void SetUsername(string? username)
    {
        if (!string.IsNullOrEmpty(username))
        {
            UsernameLabel.Text = username;
            UserInitialLabel.Text = username[0].ToString().ToUpper();
        }
        else
        {
            UsernameLabel.Text = "User";
            UserInitialLabel.Text = "U";
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var authService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();
        if (authService != null)
        {
            authService.Logout();
        }

        // Close the flyout
        Shell.Current.FlyoutIsPresented = false;

        // Navigate to login page
        await Shell.Current.GoToAsync("//Login");
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        await LoadUsernameAsync();
    }

    private async Task LoadUsernameAsync()
    {
        var authService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();
        if (authService != null)
        {
            var username = await authService.GetUsernameAsync();
            SetUsername(username);
        }
    }
}
