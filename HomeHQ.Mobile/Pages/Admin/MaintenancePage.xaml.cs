using HomeHQ.Mobile.ViewModels.Admin;

namespace HomeHQ.Mobile.Pages.Admin;

public partial class MaintenancePage : ContentPage
{
    private readonly MaintenanceViewModel _viewModel;

    public MaintenancePage(MaintenanceViewModel viewModel)
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
            await DisplayAlertAsync("Error", $"Failed to load maintenance data: {ex.Message}", "OK");
        }
    }
}
