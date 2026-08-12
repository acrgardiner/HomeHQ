namespace HomeHQ.Application.Assets;

public record AssetDashboardStats(
    int TotalAssets,
    int WarrantiesExpiringSoon,
    int DistinctCategoriesWithAssets);
