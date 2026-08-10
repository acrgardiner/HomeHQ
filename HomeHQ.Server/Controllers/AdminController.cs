using System.Linq.Expressions;
using System.Text.Json;
using HomeHQ.DTOs;
using HomeHQ.Entities;
using HomeHQ.Identity;
using HomeHQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(
    IUserService userService,
    IEntityService<Attachment> attachmentService,
    IEntityService<Asset> assetService,
    IPurgeService purgeService,
    IAssetImportService assetImportService,
    IThumbnailService thumbnailService,
    ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard()
    {
        try
        {
            var userCountTask = userService.GetUserCountAsync();
            var attachmentCountTask = attachmentService.CountAsync();
            var totalSizeTask = attachmentService.SumAsync(a => a.FileSize);
            var largeFilesTask = attachmentService.GetAsync(
                orderby: a => a.FileSize,
                descending: true,
                take: 10,
                includes: [x => x.Parent!]);
            var dateFrom = DateTime.UtcNow;
            var dateTo = DateTime.UtcNow.AddYears(1);
            var warrantiesTask = assetService.GetAsync(
                filter: x => x.WarrantyExpiration.HasValue
                             && x.WarrantyExpiration > dateFrom
                             && x.WarrantyExpiration < dateTo,
                orderby: a => a.WarrantyExpiration!,
                descending: false,
                take: 10,
                includes: new List<Expression<Func<Asset, object>>> { x => x.Category!, x => x.WarrantyType! });
            var purgeTask = purgeService.GetSoftDeletedCountAsync();

            await Task.WhenAll(
                userCountTask,
                attachmentCountTask,
                totalSizeTask,
                largeFilesTask,
                warrantiesTask,
                purgeTask);

            var largeAttachments = (await largeFilesTask).Select(a => new LargeAttachmentDto(
                a.Id,
                a.OriginFileName,
                a.FileSize,
                a.ParentId,
                a.Parent?.Name)).ToList();

            var warranties = (await warrantiesTask).Select(a => new ExpiringWarrantyDto(
                a.Id,
                a.Name,
                a.WarrantyExpiration,
                a.Category?.Name,
                a.WarrantyType?.Name)).ToList();

            var pendingPurge = (await purgeTask)
                .Select(kvp => new SoftDeletedCountDto(kvp.Key, kvp.Value))
                .ToList();

            var dto = new AdminDashboardDto(
                await userCountTask,
                await attachmentCountTask,
                await totalSizeTask,
                largeAttachments,
                warranties,
                pendingPurge);

            return Ok(ApiResponse<AdminDashboardDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading admin dashboard");
            return Ok(ApiResponse<AdminDashboardDto>.Fail("Error loading admin dashboard."));
        }
    }

    [HttpGet("purge/counts")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SoftDeletedCountDto>>>> GetPurgeCounts()
    {
        try
        {
            var counts = await purgeService.GetSoftDeletedCountAsync();
            var dto = counts.Select(kvp => new SoftDeletedCountDto(kvp.Key, kvp.Value)).ToList();
            return Ok(ApiResponse<IEnumerable<SoftDeletedCountDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading soft-deleted counts");
            return Ok(ApiResponse<IEnumerable<SoftDeletedCountDto>>.Fail("Error loading soft-deleted counts."));
        }
    }

    [HttpPost("purge")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SoftDeletedCountDto>>>> PurgeAll()
    {
        try
        {
            var result = await purgeService.PurgeAllSoftDeletedRecordsAsync();
            var dto = result.Select(kvp => new SoftDeletedCountDto(kvp.Key, kvp.Value)).ToList();
            var total = result.Values.Sum();
            return Ok(ApiResponse<IEnumerable<SoftDeletedCountDto>>.Ok(
                dto,
                total > 0 ? $"Successfully purged {total} records" : "No records were purged"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error purging soft-deleted records");
            return Ok(ApiResponse<IEnumerable<SoftDeletedCountDto>>.Fail($"Error purging records: {ex.Message}"));
        }
    }

    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<int>>> ImportAssets()
    {
        try
        {
            var importedCount = await assetImportService.ImportAssets();
            return Ok(ApiResponse<int>.Ok(importedCount, $"{importedCount} assets imported successfully"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing assets");
            return Ok(ApiResponse<int>.Fail($"Error importing assets: {ex.Message}"));
        }
    }

    [HttpPost("thumbnails/rebuild")]
    public async Task<ActionResult<ApiResponse<int>>> RebuildThumbnails()
    {
        try
        {
            var thumbCount = await thumbnailService.RebuildAll();
            return Ok(ApiResponse<int>.Ok(thumbCount, $"Successfully rebuilt {thumbCount} thumbnails"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rebuilding thumbnails");
            return Ok(ApiResponse<int>.Fail($"Error rebuilding thumbnails: {ex.Message}"));
        }
    }

    [HttpGet("logs")]
    public ActionResult<ApiResponse<IEnumerable<string>>> GetLogFiles()
    {
        try
        {
            var logDirectory = Path.Combine("appdata", "logs");
            if (!Directory.Exists(logDirectory))
            {
                return Ok(ApiResponse<IEnumerable<string>>.Ok([]));
            }

            var files = Directory.GetFiles(logDirectory, "log-*.json")
                .OrderByDescending(f => f)
                .Select(Path.GetFileName)
                .Where(f => f is not null)
                .Cast<string>()
                .ToList();

            return Ok(ApiResponse<IEnumerable<string>>.Ok(files));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing log files");
            return Ok(ApiResponse<IEnumerable<string>>.Fail($"Error listing log files: {ex.Message}"));
        }
    }

    [HttpGet("logs/{fileName}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LogEntryDto>>>> GetLogEntries(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Contains("..", StringComparison.Ordinal)
            || fileName.Contains('/') || fileName.Contains('\\')
            || !fileName.StartsWith("log-", StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(ApiResponse<IEnumerable<LogEntryDto>>.Fail("Invalid log file name."));
        }

        try
        {
            var file = Path.Combine("appdata", "logs", fileName);
            if (!System.IO.File.Exists(file))
            {
                return Ok(ApiResponse<IEnumerable<LogEntryDto>>.Fail("Log file not found."));
            }

            var entries = new List<LogEntryDto>();
            await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            while (await reader.ReadLineAsync() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                entries.Add(new LogEntryDto(
                    root.TryGetProperty("Timestamp", out var ts) ? ts.GetString() ?? "" : "",
                    root.TryGetProperty("Level", out var level) ? level.GetString() ?? "" : "",
                    root.TryGetProperty("SourceContext", out var sc) ? sc.GetString() ?? "" : "",
                    root.TryGetProperty("Message", out var msg) ? msg.GetString() ?? "" : "",
                    root.TryGetProperty("Exception", out var exProp) ? exProp.GetString() ?? "" : ""));
            }

            entries.Reverse();
            return Ok(ApiResponse<IEnumerable<LogEntryDto>>.Ok(entries));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading log file {FileName}", fileName);
            return Ok(ApiResponse<IEnumerable<LogEntryDto>>.Fail($"Failed to read log file: {ex.Message}"));
        }
    }
}
