using System.Net.Http.Json;
using HomeHQ.DTOs;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// Generic singleton cache service for reference / lookup entities (e.g. Category,
/// AttachmentType, WarrantyType).  Fetches all items from the API once and serves
/// subsequent requests from an in-memory dictionary keyed by <see cref="Guid"/> Id.
/// </summary>
public class CacheService<T> where T : class, IEntityDto
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

    public async Task<IReadOnlyList<T>> GetAllAsync(bool forceRefresh = false)
    {
        await EnsureLoadedAsync(forceRefresh);
        return _items.AsReadOnly();
    }

    public async Task<IReadOnlyDictionary<Guid, T>> GetLookupAsync(bool forceRefresh = false)
    {
        await EnsureLoadedAsync(forceRefresh);
        return _lookup;
    }

    public T? GetById(Guid? id)
        => id.HasValue && _lookup.TryGetValue(id.Value, out var item) ? item : null;

    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh)
        {
            return;
        }

        await _loadLock.WaitAsync();
        try
        {
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

    public void ClearCache()
    {
        _lookup.Clear();
        _items.Clear();
        _isLoaded = false;
    }

    public void Populate<TParent>(
        TParent item,
        Func<TParent, Guid?> idSelector,
        Action<TParent, T?> setter)
        => setter(item, GetById(idSelector(item)));

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
            if (!_isLoaded)
            {
                _lookup = [];
                _items = [];
            }
        }
    }
}
