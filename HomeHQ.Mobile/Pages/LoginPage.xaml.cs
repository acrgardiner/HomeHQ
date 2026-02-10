using HomeHQ.Mobile.Services;
using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly SharedImageService _sharedImageService;

    public LoginPage(LoginViewModel viewModel, SharedImageService sharedImageService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _sharedImageService = sharedImageService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check if already authenticated
        if (BindingContext is LoginViewModel vm && await vm.CheckAuthenticationAsync())
        {
            // Check for pending shared image
            if (CheckAndLoadPendingImage())
            {
                // Navigate to NewAsset page with the pending image
                await Shell.Current.GoToAsync("//Assets/Create");
            }
            else
            {
                // No pending image, go to Dashboard
                await Shell.Current.GoToAsync("//Dashboard");
            }
        }
        // If not authenticated, stay on login page (do nothing)
    }

    /// <summary>
    /// Checks for pending image in Preferences and loads it into SharedImageService.
    /// Returns true if a pending image was found.
    /// </summary>
    private bool CheckAndLoadPendingImage()
    {
        try
        {
            var imagePath = Preferences.Get("pending_image_path", string.Empty);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                var fileName = Preferences.Get("pending_image_filename", string.Empty);
                var contentType = Preferences.Get("pending_image_contenttype", string.Empty);

                // Load into shared service
                _sharedImageService.SetPendingImage(
                    imagePath,
                    string.IsNullOrEmpty(fileName) ? null : fileName,
                    string.IsNullOrEmpty(contentType) ? null : contentType);

                // Clear preferences
                Preferences.Remove("pending_image_path");
                Preferences.Remove("pending_image_filename");
                Preferences.Remove("pending_image_contenttype");

                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking for pending image: {ex}");
        }

        return false;
    }
}
