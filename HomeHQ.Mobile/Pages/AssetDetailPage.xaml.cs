using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class AssetDetailPage : ContentPage
{
    private readonly AssetDetailViewModel _viewModel;

    public AssetDetailPage(AssetDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Only reload if asset is not already loaded (e.g., when navigating back)
            if (_viewModel.Asset == null && !string.IsNullOrEmpty(_viewModel.AssetId))
            {
                await _viewModel.LoadAssetAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load asset: {ex.Message}", "OK");
        }
    }
}
