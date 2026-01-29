using HomeHQ.Entities;
using HomeHQ.Services;
using HomeHQ.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BearerAndCookies")]
public class AssetsController : EntitiesController<Asset>
{
    private readonly IAssetImportService _assetImportService;
    public AssetsController(IEntityService<Asset> entityService, IAssetImportService assetImportService) : base(entityService)
    {
        _assetImportService = assetImportService;
    }

    [HttpGet("import")]
    public async Task<ActionResult<ApiResponse<int>>> ImportAssets()
    {
        var importedCount = await _assetImportService.ImportAssets();
        return Ok(ApiResponse<int>.Ok(importedCount, $"{importedCount} assets imported successfully"));
    }
}
