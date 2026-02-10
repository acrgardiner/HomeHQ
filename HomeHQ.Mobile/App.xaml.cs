using HomeHQ.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomeHQ.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Handle pending shared image navigation after Shell is ready
            window.Created += async (s, e) =>
            {
                // Give Shell time to initialize
                await Task.Delay(500);
                await HandlePendingSharedImageAsync();
            };

            return window;
        }

        private async Task HandlePendingSharedImageAsync()
        {
            try
            {
                var sharedImageService = Handler?.MauiContext?.Services.GetService<SharedImageService>();
                if (sharedImageService == null) return;

                // Check for pending image from Preferences (set by ShareActivity)
                LoadPendingImageFromPreferences(sharedImageService);

                if (sharedImageService.HasPendingImage && Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("NewAsset");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling pending shared image: {ex}");
            }
        }

        private static void LoadPendingImageFromPreferences(SharedImageService sharedImageService)
        {
            try
            {
                var imagePath = Preferences.Get("pending_image_path", string.Empty);
                var fileName = Preferences.Get("pending_image_filename", string.Empty);
                var contentType = Preferences.Get("pending_image_contenttype", string.Empty);

                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    sharedImageService.SetPendingImage(
                        imagePath,
                        string.IsNullOrEmpty(fileName) ? null : fileName,
                        string.IsNullOrEmpty(contentType) ? null : contentType);

                    // Clear the preferences after reading
                    Preferences.Remove("pending_image_path");
                    Preferences.Remove("pending_image_filename");
                    Preferences.Remove("pending_image_contenttype");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading pending image from preferences: {ex}");
            }
        }
    }
}