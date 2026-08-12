namespace HomeHQ.Services;

public interface IPurgeService
{
    Task<Dictionary<string, int>> PurgeAllSoftDeletedRecordsAsync();
    Task<Dictionary<string, int>> PurgeSoftDeletedRecordsAsync(DateTime deletedBefore);
    Task<Dictionary<string, int>> GetSoftDeletedCountAsync();
}
