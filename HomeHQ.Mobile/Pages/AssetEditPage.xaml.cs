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
            // Reload when the page appears if the asset is not already loaded
            if (!_viewModel.ShowContent && !string.IsNullOrEmpty(_viewModel.AssetId))
            {
                await _viewModel.LoadAsync();
            }
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
