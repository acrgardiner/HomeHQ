using HomeHQ.Entities;
using HomeHQ.Services;

namespace HomeHQ.Application.Assets;

public class GetAssetDashboardStatsHandler : IGetAssetDashboardStatsHandler
{
    private readonly IEntityService<Asset> _assetService;

    public GetAssetDashboardStatsHandler(IEntityService<Asset> assetService)
    {
        _assetService = assetService;
    }

    public async Task<AssetDashboardStats> GetAsync(int expiringWithinDays = 30, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var expiringThrough = today.AddDays(expiringWithinDays);

        cancellationToken.ThrowIfCancellationRequested();

        var totalAssetsTask = _assetService.CountAsync();
        var expiringTask = _assetService.CountAsync(
            a => a.WarrantyExpiration.HasValue
                 && a.WarrantyExpiration.Value.Date >= today
                 && a.WarrantyExpiration.Value.Date <= expiringThrough);
        var categoryGroupsTask = _assetService.GetCountByPropertyAsync(
            a => a.CategoryId!,
            filter: a => a.CategoryId != null);

        await Task.WhenAll(totalAssetsTask, expiringTask, categoryGroupsTask);

        return new AssetDashboardStats(
            await totalAssetsTask,
            await expiringTask,
            (await categoryGroupsTask).Count);
    }
}
