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
            // Refresh carousel preview after attachment viewer (rotate/crop updates PendingUploadBytes).
            await _viewModel.LoadCurrentAttachmentPreviewAsync();
            _viewModel.NotifyAttachmentCarouselChanged();
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
