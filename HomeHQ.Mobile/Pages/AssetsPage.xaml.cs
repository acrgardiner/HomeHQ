using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class AssetsPage : ContentPage
{
    private readonly AssetsViewModel _viewModel;

    public AssetsPage(AssetsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAssetsAsync();
    }
}
