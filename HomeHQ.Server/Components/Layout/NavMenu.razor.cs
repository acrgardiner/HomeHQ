using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace HomeHQ.Server.Components.Layout;

public partial class NavMenu
{
    private string? currentUrl;
    private int _assetsCount = 0;

    protected override void OnInitialized()
    {
        currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
        // Load assets count for badge
        _ = LoadAssetCount();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    private async Task LoadAssetCount()
    {
        try
        {
            // This would ideally come from a service injection
            // For now, we'll use a placeholder
            _assetsCount = await AssetService.CountAsync();
            StateHasChanged();
        }
        catch
        {
            // Handle silently
        }
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
