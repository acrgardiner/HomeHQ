using HomeHQ.Mobile.Services;

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

            // Check for pending shared image when app is activated/resumed
            window.Activated += OnWindowActivated;

            return window;
        }

        private async void OnWindowActivated(object? sender, EventArgs e)
        {
            // Small delay to ensure Shell is ready
            await Task.Delay(300);
            
            try
            {
                var sharedImageService = Handler?.MauiContext?.Services.GetService<SharedImageService>();
                if (sharedImageService == null) return;

                // Check preferences for pending image
                var imagePath = Preferences.Get("pending_image_path", string.Empty);

                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    var currentRoute = Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
                    
                    // Skip if already on Create page or Login page
                    if (currentRoute.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
                        currentRoute.Contains("Login", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    var fileName = Preferences.Get("pending_image_filename", string.Empty);
                    var contentType = Preferences.Get("pending_image_contenttype", string.Empty);

                    // Load into shared service
                    sharedImageService.SetPendingImage(
                        imagePath,
                        string.IsNullOrEmpty(fileName) ? null : fileName,
                        string.IsNullOrEmpty(contentType) ? null : contentType);

                    // Clear preferences
                    Preferences.Remove("pending_image_path");
                    Preferences.Remove("pending_image_filename");
                    Preferences.Remove("pending_image_contenttype");

                    // Navigate to AssetCreatePage
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("//Assets/Create");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for pending image on activation: {ex}");
            }
        }
    }
}
