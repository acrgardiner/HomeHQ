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
            // Refresh the current attachment preview in case it was modified in
            // the attachment viewer page when navigating back.
            //await _viewModel.LoadCurrentAttachmentPreviewAsync();
            //_viewModel.NotifyAttachmentCarouselChanged();

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
