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
            // Only reload if asset is not already loaded, or if signaled to refresh
            if ((_viewModel.Asset == null && !string.IsNullOrEmpty(_viewModel.AssetId)) || (_viewModel.Refresh))
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
