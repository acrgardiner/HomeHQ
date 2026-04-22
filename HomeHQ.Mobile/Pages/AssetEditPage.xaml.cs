using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class AssetEditPage : ContentPage
{
    private readonly AssetEditViewModel _viewModel;

    public AssetEditPage(AssetEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            //Actioned within setter
            //await _viewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load asset: {ex.Message}", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
