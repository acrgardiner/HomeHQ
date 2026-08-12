using HomeHQ.Mobile.ViewModels.Admin;

namespace HomeHQ.Mobile.Pages.Admin;

public partial class UserEditPage : ContentPage
{
    private readonly UserEditViewModel _viewModel;

    public UserEditPage(UserEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
