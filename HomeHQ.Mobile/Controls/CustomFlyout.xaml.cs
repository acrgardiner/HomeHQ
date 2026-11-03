using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Controls;

public partial class CustomFlyout : ContentView
{
    public CustomFlyout()
    {
        InitializeComponent();
    }

    public FlyoutViewModel? ViewModel => BindingContext as FlyoutViewModel;

    public void SetUsername(string? username) => FlyoutFooterControl.SetUsername(username);
}
