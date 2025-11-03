using Microsoft.EntityFrameworkCore;
using projectaardvarkx2.Contracts;
using projectaardvarkx2.Data;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.Services;

public class PurgeService : IPurgeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PurgeService> _logger;

    public PurgeService(ApplicationDbContext context, ILogger<PurgeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<string, int>> PurgeAllSoftDeletedRecordsAsync()
    {
        return await PurgeSoftDeletedRecordsAsync(DateTime.MaxValue);
    }

    public async Task<Dictionary<string, int>> PurgeSoftDeletedRecordsAsync(DateTime deletedBefore)
    {
        var result = new Dictionary<string, int>();

        try
        {
            _logger.LogInformation("Starting purge of soft-deleted records deleted before {DeletedBefore}", deletedBefore);

            // Purge each entity type that implements ISoftDelete
            result["Assets"] = await PurgeEntityAsync<Asset>(deletedBefore);
            result["Categories"] = await PurgeEntityAsync<Category>(deletedBefore);
            result["WarrantyTypes"] = await PurgeEntityAsync<WarrantyType>(deletedBefore);
            result["AttachmentTypes"] = await PurgeEntityAsync<AttachmentType>(deletedBefore);
            result["Attributes"] = await PurgeEntityAsync<AttributeValue>(deletedBefore);
            result["Notes"] = await PurgeEntityAsync<Note>(deletedBefore);

            //Also need to delete files (including thumbnails) associated with Attachments
            result["Attachments"] = await PurgeEntityAsync<Attachment>(deletedBefore);

            var totalPurged = result.Values.Sum();
            _logger.LogInformation("Purge completed. Total records purged: {TotalPurged}", totalPurged);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while purging soft-deleted records");
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetSoftDeletedCountAsync()
    {
        var result = new Dictionary<string, int>();

        try
        {
            result["Assets"] = await GetSoftDeletedCountAsync<Asset>();
            result["Categories"] = await GetSoftDeletedCountAsync<Category>();
            result["WarrantyTypes"] = await GetSoftDeletedCountAsync<WarrantyType>();
            result["Attachments"] = await GetSoftDeletedCountAsync<Attachment>();
            result["AttachmentTypes"] = await GetSoftDeletedCountAsync<AttachmentType>();
            result["Attributes"] = await GetSoftDeletedCountAsync<AttributeValue>();
            result["Notes"] = await GetSoftDeletedCountAsync<Note>();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting soft-deleted counts");
            throw;
        }
    }

    private async Task<int> PurgeEntityAsync<T>(DateTime deletedBefore) where T : AuditableEntity
    {
        var dbSet = _context.Set<T>();
        
        // Find all soft-deleted records
        var entitiesToPurge = await dbSet
            .IgnoreQueryFilters()
            .Where(e => e.DeletedOn != null && e.DeletedOn < deletedBefore)
            .ToListAsync();

        if (entitiesToPurge.Any())
        {
            _logger.LogInformation("Purging {Count} {EntityType} records", entitiesToPurge.Count, typeof(T).Name);
            
            // Permanently delete the entities
            dbSet.RemoveRange(entitiesToPurge);
            await _context.SaveChangesAsync();
        }

        return entitiesToPurge.Count;
    }

    private async Task<int> GetSoftDeletedCountAsync<T>() where T : AuditableEntity
    {
        var dbSet = _context.Set<T>();
        
        return await dbSet
            .IgnoreQueryFilters()
            .Where(e => e.DeletedOn != null)
            .CountAsync();
    }
}


