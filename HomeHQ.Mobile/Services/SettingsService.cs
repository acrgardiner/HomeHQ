using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HomeHQ.Mobile.Models;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// Service for managing user-configurable app settings.
/// </summary>
public class SettingsService
{
    private const string API_BASE_URL_KEY = "api_base_url";
    private const string CONNECTIONS_KEY = "connections";
    private const string SELECTED_CONNECTION_KEY = "selected_connection";
    private const string DEFAULT_API_URL = "https://homehq-demo.duckdns.org/";
    private const string DEFAULT_CONNECTION_NAME = "DEMO";

    private List<Connection>? _connections;

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

    public IReadOnlyList<Connection> Connections => _connections ??= LoadConnections();

    public Connection SelectedConnection =>
        Connections.FirstOrDefault(connection => connection.Url.Equals(SelectedConnectionUrl, StringComparison.OrdinalIgnoreCase))
        ?? Connections[0];

    private string SelectedConnectionUrl => Preferences.Default.Get(SELECTED_CONNECTION_KEY, DEFAULT_API_URL);

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

    public void SelectConnection(Connection connection)
    {
        var storedConnection = Connections.FirstOrDefault(item =>
            item.Url.Equals(connection.Url, StringComparison.OrdinalIgnoreCase));
        if (storedConnection is null)
        {
            return;
        }

        Preferences.Default.Set(SELECTED_CONNECTION_KEY, storedConnection.Url);
        SetApiBaseUrl(storedConnection.Url);
    }

    public Connection AddConnection(string name, string url)
    {
        var connection = new Connection { Name = name.Trim(), Url = NormalizeUrl(url) };
        _connections!.Add(connection);
        SaveConnections();
        SelectConnection(connection);
        return connection;
    }

    public bool RemoveConnection(Connection connection)
    {
        if (connection.Fixed || !_connections!.Remove(connection))
        {
            return false;
        }

        SaveConnections();
        if (SelectedConnectionUrl.Equals(connection.Url, StringComparison.OrdinalIgnoreCase))
        {
            SelectConnection(Connections[0]);
        }

        return true;
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

    private List<Connection> LoadConnections()
    {
        List<Connection>? connections = null;
        var storedConnections = Preferences.Default.Get(CONNECTIONS_KEY, string.Empty);

        if (!string.IsNullOrWhiteSpace(storedConnections))
        {
            try
            {
                connections = JsonSerializer.Deserialize<List<Connection>>(storedConnections);
            }
            catch (JsonException)
            {
                connections = null;
            }
        }

        connections ??= [];
        if (true) //!connections.Any(connection => connection.Fixed))
        {
            connections.Insert(0, new Connection
            {
                Name = DEFAULT_CONNECTION_NAME,
                Url = DEFAULT_API_URL,
                Fixed = true
            });

#if DEBUG
            connections.Insert(1, new Connection
            {
                Name = "LocalDev",
                Url = "https://192.168.0.151:7176",
                Fixed = true
            });
#endif
        }

        var legacyUrl = Preferences.Default.Get(API_BASE_URL_KEY, string.Empty);
        Connection? migratedConnection = null;
        if (!string.IsNullOrWhiteSpace(legacyUrl) &&
            !legacyUrl.Equals(DEFAULT_API_URL, StringComparison.OrdinalIgnoreCase) &&
            !connections.Any(connection => connection.Url.Equals(legacyUrl, StringComparison.OrdinalIgnoreCase)))
        {
            migratedConnection = new Connection { Name = "My Server", Url = NormalizeUrl(legacyUrl) };
            connections.Add(migratedConnection);
        }

        SaveConnections(connections);

        var selectedUrl = Preferences.Default.ContainsKey(SELECTED_CONNECTION_KEY)
            ? SelectedConnectionUrl
            : migratedConnection?.Url ?? DEFAULT_API_URL;
        var selectedConnection = connections.FirstOrDefault(connection =>
            connection.Url.Equals(selectedUrl, StringComparison.OrdinalIgnoreCase));
        if (selectedConnection is null)
        {
            selectedConnection = connections.First();
            Preferences.Default.Set(SELECTED_CONNECTION_KEY, selectedConnection.Url);
        }

        ApiBaseUrl = selectedConnection.Url;
        return connections;
    }

    private void SaveConnections(List<Connection>? connections = null)
    {
        Preferences.Default.Set(CONNECTIONS_KEY, JsonSerializer.Serialize((connections ?? _connections)?.Where(x => !x.Fixed).ToList()));
    }

    private static string NormalizeUrl(string url)
    {
        url = url.Trim();
        return url.EndsWith('/') ? url : $"{url}/";
    }
}
