using System.Diagnostics.CodeAnalysis;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// Service for managing user-configurable app settings.
/// </summary>
public class SettingsService
{
    private const string API_BASE_URL_KEY = "api_base_url";
    private const string DEFAULT_API_URL = "https://your-server-url/";

    /// <summary>
    /// Gets the configured API base URL.
    /// </summary>
    [AllowNull]
    public string ApiBaseUrl
    {
        get
        {
            field ??= Preferences.Default.Get(API_BASE_URL_KEY, DEFAULT_API_URL);
            return field;
        }

        private set;
    }

    /// <summary>
    /// Sets the API base URL.
    /// </summary>
    public void SetApiBaseUrl(string url)
    {
        // Ensure URL ends with /
        if (!url.EndsWith('/'))
        {
            url += "/";
        }

        Preferences.Default.Set(API_BASE_URL_KEY, url);
        ApiBaseUrl = url;

        // Notify that settings changed
        OnApiBaseUrlChanged?.Invoke(this, url);
    }

    /// <summary>
    /// Checks if a custom API URL has been configured.
    /// </summary>
    public bool HasCustomApiUrl => Preferences.Default.ContainsKey(API_BASE_URL_KEY);

    /// <summary>
    /// Resets to default API URL.
    /// </summary>
    public void ResetApiBaseUrl()
    {
        Preferences.Default.Remove(API_BASE_URL_KEY);
        ApiBaseUrl = null;
        OnApiBaseUrlChanged?.Invoke(this, DEFAULT_API_URL);
    }

    /// <summary>
    /// Event raised when the API base URL changes.
    /// </summary>
    public event EventHandler<string>? OnApiBaseUrlChanged;
}
