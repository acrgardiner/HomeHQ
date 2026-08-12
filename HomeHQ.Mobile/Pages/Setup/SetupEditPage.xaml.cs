using HomeHQ.Mobile.ViewModels.Setup;

namespace HomeHQ.Mobile.Pages.Setup;

public partial class SetupEditPage : ContentPage
{
    public SetupEditPage(SetupEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
