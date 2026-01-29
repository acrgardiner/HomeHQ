using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.Controls;

public partial class FlyoutFooter : ContentView
{
    public FlyoutFooter()
    {
        InitializeComponent();
    }

    public void SetUsername(string? username)
    {
        if (!string.IsNullOrEmpty(username))
        {
            UsernameLabel.Text = username;
            UserInitialLabel.Text = username[0].ToString().ToUpper();
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var authService = Application.Current?.Handler?.MauiContext?.Services.GetService<AuthService>();
        if (authService != null)
        {
            authService.Logout();
        }

        // Close the flyout
        Shell.Current.FlyoutIsPresented = false;

        // Navigate to login page
        await Shell.Current.GoToAsync("//Login");
    }
}
