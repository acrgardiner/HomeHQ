using System.Net.Http.Json;
using HomeHQ.Contracts;
using HomeHQ.DTOs;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// Generic singleton cache service for reference / lookup entities (e.g. Category,
/// AttachmentType, WarrantyType).  Fetches all items from the API once and serves
/// subsequent requests from an in-memory dictionary keyed by <see cref="Guid"/> Id.
/// </summary>
/// <typeparam name="T">
/// An <see cref="AuditableEntity"/> (provides <c>Guid Id</c>) returned by the API.
/// </typeparam>
public class CacheService<T> where T : AuditableEntity
{
    private readonly ApiClient _apiClient;
    private readonly string _endpoint;

    private Dictionary<Guid, T> _lookup = [];
    private List<T> _items = [];
    private bool _isLoaded;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public CacheService(ApiClient apiClient, string endpoint)
    {
        _apiClient = apiClient;
        _endpoint = endpoint;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all cached items, loading from the API first if the cache is empty.
    /// </summary>
    public async Task<IReadOnlyList<T>> GetAllAsync(bool forceRefresh = false)
    {
        await EnsureLoadedAsync(forceRefresh);
        return _items.AsReadOnly();
    }

    /// <summary>
    /// Returns the full lookup dictionary (Id → item), loading from the API first
    /// if the cache is empty.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, T>> GetLookupAsync(bool forceRefresh = false)
    {
        await EnsureLoadedAsync(forceRefresh);
        return _lookup;
    }

    /// <summary>
    /// Synchronous lookup by Id.  Returns <c>null</c> when the id is null, not found,
    /// or the cache has not yet been loaded.
    /// </summary>
    public T? GetById(Guid? id)
        => id.HasValue && _lookup.TryGetValue(id.Value, out var item) ? item : null;

    /// <summary>
    /// Ensures the cache is populated.  Safe to call concurrently — only one load
    /// will be issued to the API at a time.
    /// </summary>
    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh)
        {
            return;
        }

        await _loadLock.WaitAsync();
        try
        {
            // Double-check inside the lock to avoid redundant requests.
            if (_isLoaded && !forceRefresh)
            {
                return;
            }

            await LoadAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Clears the in-memory cache.  The next call to <see cref="EnsureLoadedAsync"/>
    /// will re-fetch from the API.
    /// </summary>
    public void ClearCache()
    {
        _lookup.Clear();
        _items.Clear();
        _isLoaded = false;
    }

    // ── Navigation-property population ───────────────────────────────────────

    /// <summary>
    /// Resolves the cached <typeparamref name="T"/> for <paramref name="item"/> and
    /// passes it to <paramref name="setter"/>.
    /// <para>
    /// Example:
    /// <code>_categoryCache.Populate(asset, a => a.CategoryId, (a, c) => a.Category = c);</code>
    /// </para>
    /// </summary>
    public void Populate<TParent>(
        TParent item,
        Func<TParent, Guid?> idSelector,
        Action<TParent, T?> setter)
        => setter(item, GetById(idSelector(item)));

    /// <summary>
    /// Calls <see cref="Populate{TParent}"/> for every item in <paramref name="items"/>.
    /// <para>
    /// Example:
    /// <code>_categoryCache.PopulateAll(assets, a => a.CategoryId, (a, c) => a.Category = c);</code>
    /// </para>
    /// </summary>
    public void PopulateAll<TParent>(
        IEnumerable<TParent> items,
        Func<TParent, Guid?> idSelector,
        Action<TParent, T?> setter)
    {
        foreach (var item in items)
        {
            Populate(item, idSelector, setter);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync(_endpoint);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<T>>>();
                if (apiResponse?.Data != null)
                {
                    _items = apiResponse.Data;
                    _lookup = _items.ToDictionary(i => i.Id);
                    _isLoaded = true;
                }
            }
        }
        catch
        {
            // Keep whatever we already have; leave _isLoaded false so the next
            // call will retry.
            if (!_isLoaded)
            {
                _lookup = [];
                _items = [];
            }
        }
    }
}
