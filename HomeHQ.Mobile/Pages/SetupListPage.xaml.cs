using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class SetupListPage : ContentPage
{
    private readonly SetupListViewModel _viewModel;

    public SetupListPage(SetupListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load: {ex.Message}", "OK");
        }
    }
}
