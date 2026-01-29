using System.Net.Http.Json;
using HomeHQ.DTOs;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// Shared service for managing categories lookup across the app.
/// Registered as a singleton to cache categories.
/// </summary>
public class CategoryService
{
    private readonly ApiClient _apiClient;
    private Dictionary<Guid, Category> _categoriesLookup = new();
    private List<Category> _categories = [];
    private bool _isLoaded;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public CategoryService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Gets all categories. Loads from API if not already cached.
    /// </summary>
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(bool forceRefresh = false)
    {
        await EnsureLoadedAsync(forceRefresh);
        return _categories.AsReadOnly();
    }

    /// <summary>
    /// Gets the categories lookup dictionary. Loads from API if not already cached.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, Category>> GetCategoriesLookupAsync(bool forceRefresh = false)
    {
        await EnsureLoadedAsync(forceRefresh);
        return _categoriesLookup;
    }

    /// <summary>
    /// Looks up a category by ID. Returns null if not found.
    /// </summary>
    public Category? GetCategory(Guid? categoryId)
    {
        if (!categoryId.HasValue) return null;
        return _categoriesLookup.TryGetValue(categoryId.Value, out var category) ? category : null;
    }

    /// <summary>
    /// Populates the Category navigation property on an asset if not already set.
    /// </summary>
    public void PopulateCategory(Asset asset)
    {
        if (asset.Category == null && asset.CategoryId.HasValue)
        {
            asset.Category = GetCategory(asset.CategoryId);
        }
    }

    /// <summary>
    /// Populates categories on a collection of assets.
    /// </summary>
    public void PopulateCategories(IEnumerable<Asset> assets)
    {
        foreach (var asset in assets)
        {
            PopulateCategory(asset);
        }
    }

    /// <summary>
    /// Ensures categories are loaded. Thread-safe.
    /// </summary>
    public async Task EnsureLoadedAsync(bool forceRefresh = false)
    {
        if (_isLoaded && !forceRefresh) return;

        await _loadLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_isLoaded && !forceRefresh) return;

            await LoadCategoriesAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Clears the cache. Useful for logout scenarios.
    /// </summary>
    public void ClearCache()
    {
        _categoriesLookup.Clear();
        _categories.Clear();
        _isLoaded = false;
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync("api/categories");
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<Category>>>();
                if (apiResponse?.Data != null)
                {
                    _categories = apiResponse.Data;
                    _categoriesLookup = _categories.ToDictionary(c => c.Id);
                    _isLoaded = true;
                }
            }
        }
        catch
        {
            // Keep existing data if load fails
            if (!_isLoaded)
            {
                _categoriesLookup = new Dictionary<Guid, Category>();
                _categories = [];
            }
        }
    }
}
