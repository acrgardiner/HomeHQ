using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class NewAssetPage : ContentPage
{
    private readonly NewAssetViewModel _viewModel;

    public NewAssetPage(NewAssetViewModel viewModel)
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