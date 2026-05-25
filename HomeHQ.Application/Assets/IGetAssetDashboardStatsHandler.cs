namespace HomeHQ.Application.Assets;

public interface IGetAssetDashboardStatsHandler
{
    /// <param name="expiringWithinDays">Count warranties expiring between today and today + this many days (inclusive).</param>
    Task<AssetDashboardStats> GetAsync(int expiringWithinDays = 30, CancellationToken cancellationToken = default);
}
