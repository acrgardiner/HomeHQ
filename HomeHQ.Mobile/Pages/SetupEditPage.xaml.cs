using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class SetupEditPage : ContentPage
{
    public SetupEditPage(SetupEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
