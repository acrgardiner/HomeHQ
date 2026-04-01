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

            // Copy image to cache
            var (filePath, fileName) = CopySharedFileToCache(fileUri);
            if (string.IsNullOrEmpty(filePath))
            {
                ShowToast("Failed to process share");
                Finish();
                return;
            }

            // Get API settings and auth token
            var baseUrl = Preferences.Get("api_base_url", "");
            var token = await SecureStorage.GetAsync("access_token");

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(token))
            {
                // Not configured or not logged in - open app to login
                ShowToast("Please login to HomeHQ first");
                StartActivity(new Intent(this, typeof(MainActivity)));
                Finish();
                return;
            }

            ShowToast("Creating asset...");

            using var client = new HttpClient(new HttpClientHandler
            {
#if DEBUG
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#endif
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 1. Create the asset with the filename as the default name
            var assetName = Path.GetFileNameWithoutExtension(fileName) ?? $"Shared {DateTime.Now:yyyy-MM-dd}";
            var asset = new Asset
            {
                Name = assetName,
                PurchaseDate = DateTime.Today,
                // Audit fields required for model validation (server overwrites these)
                CreatedBy = "",
                CreatedOn = DateTime.UtcNow,
                LastModifiedBy = "",
                LastModifiedOn = DateTime.UtcNow
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var assetJson = JsonSerializer.Serialize(asset, jsonOptions);

            var assetContent = new StringContent(assetJson, Encoding.UTF8, "application/json");
            var assetResponse = await client.PostAsync($"{baseUrl}api/assets", assetContent);

            if (!assetResponse.IsSuccessStatusCode)
            {
                var error = await assetResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Asset creation failed: {assetResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Request body: {assetJson}");
                System.Diagnostics.Debug.WriteLine($"Response: {error}");
                ShowToast($"Failed to create asset: {assetResponse.StatusCode}");
                Finish();
                return;
            }

            var assetResponseJson = await assetResponse.Content.ReadAsStringAsync();
            var assetResult = JsonSerializer.Deserialize<ApiResponse<Asset>>(assetResponseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var assetId = assetResult?.Data?.Id;
            if (assetId == null || assetId == Guid.Empty)
            {
                ShowToast("Failed to create asset");
                Finish();
                return;
            }

            // 2. Upload the attachment
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var contentType = Intent.Type ?? "application/octet-stream";
            var extension = Path.GetExtension(fileName) ?? GetExtensionFromMimeType(contentType);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName ?? "file.bin");
            content.Add(new StringContent(assetId.ToString()!), "parentId");
            content.Add(new StringContent("Asset"), "parentType");
            content.Add(new StringContent(fileName ?? "file.bin"), "originFileName");
            content.Add(new StringContent(contentType), "contentType");
            content.Add(new StringContent(extension), "extension");

            var uploadResponse = await client.PostAsync($"{baseUrl}api/attachments/upload", content);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Attachment upload failed: {uploadResponse.StatusCode}");
                // Continue anyway - asset was created, user can add file later
            }

            // 3. Open browser to edit page
            var editUrl = $"{baseUrl}assets/{assetId}/edit";
            var browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(editUrl));
            browserIntent.AddFlags(ActivityFlags.NewTask);
            StartActivity(browserIntent);

            ShowToast("Asset created! Opening editor...");

            // Clean up temp file
            try { File.Delete(filePath); } catch { }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network error: {ex}");
            ShowToast("Network error - check your connection");
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

    private (string? path, string? fileName) CopySharedFileToCache(Android.Net.Uri uri)
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

            var cachePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using var inputStream = ContentResolver?.OpenInputStream(uri);
            if (inputStream == null)
            {
                return (null, null);
            }

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
