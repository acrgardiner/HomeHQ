using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace HomeHQ.Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            // Replace the current intent so MAUI/Startup code can read extras from MainActivity.Intent
            this.Intent = intent;

            // Handle navigation from incoming intent
            _ = HandleIntentAsync(intent);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Handle initial intent if app launched via intent
            var intent = this.Intent;
            if (intent != null)
            {
                _ = HandleIntentAsync(intent);
            }
        }

        private async Task HandleIntentAsync(Intent intent)
        {
            try
            {
                if (intent.HasExtra("asset_id"))
                {
                    var assetId = intent.GetStringExtra("asset_id");

                    // Wait for MAUI Shell to be ready
                    for (int i = 0; i < 50 && Shell.Current == null; i++)
                    {
                        await Task.Delay(100);
                    }

                    if (Shell.Current != null)
                    {
                        // Navigate to AssetEdit passing the assetId (may be Guid.Empty)
                        try
                        {
                            var param = new Dictionary<string, object>
                            {
                                { "assetId", assetId }
                            };

                            if (intent.HasExtra("share_mime_type"))
                            {
                                var mime = intent.GetStringExtra("share_mime_type");
                                if (!string.IsNullOrEmpty(mime))
                                {
                                    param["shareMimeType"] = mime;
                                }
                            }

                            await Shell.Current.GoToAsync($"AssetEdit", param);
                        }
                        catch
                        {
                            // Ignore navigation errors
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HandleIntentAsync error: {ex}");
            }
        }
    }
}
