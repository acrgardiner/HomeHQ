using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Check if already authenticated, go to Dashboard
            // AppShell.OnNavigated will handle pending image check
            if (BindingContext is LoginViewModel vm && await vm.CheckAuthenticationAsync())
            {
                await Shell.Current.GoToAsync("//Dashboard");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
    }
}
