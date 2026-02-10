using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Text;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile
{
    [Activity(Label = "Add to HomeHQ"
        , Theme = "@style/Maui.SplashTheme"
        , MainLauncher = false
        , LaunchMode = LaunchMode.SingleTask
        , Exported = true
        , ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
        , ScreenOrientation = ScreenOrientation.Portrait)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
    public class ShareActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleShareIntent();
        }

        private void HandleShareIntent()
        {
            if (Intent?.Type?.StartsWith("image/") != true)
            {
                FinishAndStartMain();
                return;
            }

            try
            {
                var imageUri = Intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
                if (imageUri == null)
                {
                    FinishAndStartMain();
                    return;
                }

                // Copy the shared image to app's cache directory for reliable access
                var (imagePath, fileName) = CopyImageToCache(imageUri);

                if (string.IsNullOrEmpty(imagePath))
                {
                    FinishAndStartMain();
                    return;
                }

                Preferences.Set("pending_image_path", imagePath);
                Preferences.Set("pending_image_filename", fileName ?? "");
                Preferences.Set("pending_image_contenttype", Intent.Type ?? "image/jpeg");

                FinishAndStartMain();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling share intent: {ex}");
                FinishAndStartMain();
            }
        }

        private void FinishAndStartMain()
        {
            // Start MainActivity which will handle the pending image
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            StartActivity(intent);
            Finish();
        }


        private (string? path, string? fileName) CopyImageToCache(Android.Net.Uri uri)
        {
            try
            {
                // Try to get the original filename from the content resolver
                string? fileName = null;
                using (var cursor = ContentResolver?.Query(uri, null, null, null, null))
                {
                    if (cursor != null && cursor.MoveToFirst())
                    {
                        var nameIndex = cursor.GetColumnIndex(Android.Provider.OpenableColumns.DisplayName);
                        if (nameIndex >= 0)
                        {
                            fileName = cursor.GetString(nameIndex);
                        }
                    }
                }

                // Generate a unique filename if we couldn't get the original
                if (string.IsNullOrEmpty(fileName))
                {
                    var extension = GetExtensionFromMimeType(Intent?.Type);
                    fileName = $"shared_image_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                }

                var cachePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

                using var inputStream = ContentResolver?.OpenInputStream(uri);
                if (inputStream == null)
                    return (null, null);

                using var outputStream = System.IO.File.Create(cachePath);
                inputStream.CopyTo(outputStream);

                return (cachePath, fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying shared image: {ex}");
                return (null, null);
            }
        }

        private static string GetExtensionFromMimeType(string? mimeType)
        {
            return mimeType?.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/heic" => ".heic",
                _ => ".jpg"
            };
        }
    }
}
