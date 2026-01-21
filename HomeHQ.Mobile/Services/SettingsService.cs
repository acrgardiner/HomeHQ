namespace HomeHQ.Mobile.Services;

/// <summary>
/// Service for managing user-configurable app settings.
/// </summary>
public class SettingsService
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string DefaultApiUrl = "https://your-server-url/";

    private string? _cachedApiBaseUrl;

    /// <summary>
    /// Gets the configured API base URL.
    /// </summary>
    public string ApiBaseUrl
    {
        get
        {
            _cachedApiBaseUrl ??= Preferences.Default.Get(ApiBaseUrlKey, DefaultApiUrl);
            return _cachedApiBaseUrl;
        }
    }

    /// <summary>
    /// Sets the API base URL.
    /// </summary>
    public void SetApiBaseUrl(string url)
    {
        // Ensure URL ends with /
        if (!url.EndsWith('/'))
            url += "/";

        Preferences.Default.Set(ApiBaseUrlKey, url);
        _cachedApiBaseUrl = url;

        // Notify that settings changed
        OnApiBaseUrlChanged?.Invoke(this, url);
    }

    /// <summary>
    /// Checks if a custom API URL has been configured.
    /// </summary>
    public bool HasCustomApiUrl => Preferences.Default.ContainsKey(ApiBaseUrlKey);

    /// <summary>
    /// Resets to default API URL.
    /// </summary>
    public void ResetApiBaseUrl()
    {
        Preferences.Default.Remove(ApiBaseUrlKey);
        _cachedApiBaseUrl = null;
        OnApiBaseUrlChanged?.Invoke(this, DefaultApiUrl);
    }

    /// <summary>
    /// Event raised when the API base URL changes.
    /// </summary>
    public event EventHandler<string>? OnApiBaseUrlChanged;
}
