using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeHQ.Mobile
{
    [Activity(Label = "Add Asset", Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, ScreenOrientation = ScreenOrientation.Portrait)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
    public class ShareActivity : MauiAppCompatActivity
    {

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState); //throws Exception  

            if (Intent.Type == "text/plain")
            {

            }
            else if (Intent.Type.StartsWith("image/"))
            {
                var imageUri = (Android.Net.Uri)Intent.GetParcelableExtra(Intent.ExtraStream);
                if (imageUri != null)
                {
                    //System.Diagnostics.Debug.WriteLine($"Shared Image URI: {imageUri}");
                }
            }
        }
    }
}
