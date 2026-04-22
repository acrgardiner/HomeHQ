using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HomeHQ.Mobile;

[Activity(
    Label = "Add Asset",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = false,
    Exported = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Portrait)]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "image/*")]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "application/pdf")]
public class AddAssetActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _ = HandleShareIntentAsync();
    }

    private async Task HandleShareIntentAsync()
    {
        var type = Intent?.Type;
        if (string.IsNullOrEmpty(type) ||
            !(type.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
              type.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)))
        {
            Finish();
            return;
        }

        try
        {
            var fileUri = Intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
            if (fileUri == null)
            {
                Finish();
                return;
            }

            // Copy shared file to temporary cache
            var (filePath, fileName) = CopySharedFileToCache(fileUri, nameof(Asset));
            if (string.IsNullOrEmpty(filePath))
            {
                ShowToast("Failed to process share");
                Finish();
                return;
            }

            try
            {
                var launchIntent = new Intent(this, typeof(MainActivity));
                launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                // Put extras so MainActivity/MAUI can handle and route to the editor
                // We want to open the editor for a new asset flow (GUID.Empty)
                launchIntent.PutExtra("asset_id", Guid.Empty.ToString());
                StartActivity(launchIntent);
                //ShowToast("Asset created! Opening editor...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to launch MainActivity: {ex}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share error: {ex}");
            ShowToast("Error creating asset");
        }
        finally
        {
            Finish();
        }
    }

    private void ShowToast(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Toast.MakeText(this, message, ToastLength.Short)?.Show();
        });
    }

    private (string? path, string? fileName) CopySharedFileToCache(Android.Net.Uri uri, string parentType)
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
                fileName = $"shared_file_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            }

            var cacheDir = Path.Combine(FileSystem.CacheDirectory, nameof(Attachment), parentType, Guid.Empty.ToString());

            Directory.CreateDirectory(cacheDir);

            using var inputStream = ContentResolver?.OpenInputStream(uri);
            if (inputStream == null)
            {
                return (null, null);
            }

            var cachePath = Path.Combine(cacheDir, fileName);

            using var outputStream = File.Create(cachePath);
            inputStream.CopyTo(outputStream);

            return (cachePath, fileName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error copying shared file: {ex}");
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
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
    }
}
