using HomeHQ.Application.Assets;
using HomeHQ.Application.Mapping;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AssetsController : EntitiesController<Asset, AssetDto, CreateAssetRequest, UpdateAssetRequest>
{
    private readonly IAssetImportService _assetImportService;
    private readonly IGetAssetDashboardStatsHandler _dashboardStats;

    public AssetsController(
        IEntityService<Asset> entityService,
        IAssetImportService assetImportService,
        IGetAssetDashboardStatsHandler dashboardStats)
        : base(entityService, EntityMappings.Asset)
    {
        _assetImportService = assetImportService;
        _dashboardStats = dashboardStats;
    }

    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<ApiResponse<AssetDashboardStatsDto>>> GetDashboardStats(
        [FromQuery] int expiringWithinDays = 30)
    {
        if (expiringWithinDays < 1 || expiringWithinDays > 3650)
        {
            return BadRequest(ApiResponse<AssetDashboardStatsDto>.Fail(
                "expiringWithinDays must be between 1 and 3650."));
        }

        var stats = await _dashboardStats.GetAsync(expiringWithinDays);
        var dto = new AssetDashboardStatsDto(
            stats.TotalAssets,
            stats.WarrantiesExpiringSoon,
            stats.DistinctCategoriesWithAssets);

        return Ok(ApiResponse<AssetDashboardStatsDto>.Ok(dto));
    }

    [HttpGet("import")]
    public async Task<ActionResult<ApiResponse<int>>> ImportAssets()
    {
        var importedCount = await _assetImportService.ImportAssets();
        return Ok(ApiResponse<int>.Ok(importedCount, $"{importedCount} assets imported successfully"));
    }
}
