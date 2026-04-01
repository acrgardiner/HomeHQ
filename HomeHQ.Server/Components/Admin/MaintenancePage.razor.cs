using HomeHQ.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Admin
{
    public partial class MaintenancePage
    {
        private bool _loadingPurgeData;
        private bool _purging;
        private bool _purgeExpanded;
        private Dictionary<string, int> _softDeletedCounts = new();

        private async Task OnPurgeExpandChanged(bool isExpanded)
        {
            if (isExpanded && !_softDeletedCounts.Any())
            {
                await RefreshCountsAsync();
            }
        }

        private async Task RefreshCountsAsync()
        {
            _loadingPurgeData = true;
            try
            {
                _softDeletedCounts = await PurgeService.GetSoftDeletedCountAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error loading soft-deleted counts", Severity.Error);
                Logger.LogError(ex, "Error loading soft-deleted counts");
            }
            finally
            {
                _loadingPurgeData = false;
            }
        }

        private async Task PurgeAllAsync()
        {
            var totalRecords = _softDeletedCounts.Values.Sum();

            var confirmed = await DialogService.ShowMessageBoxAsync(
                "Confirm Purge All",
                new MarkupString($"<p>Are you sure you want to permanently delete <strong>all {totalRecords}</strong> soft-deleted records?</p>" +
                               "<p style='color: var(--mud-palette-error);'><strong>Warning:</strong> This action cannot be undone!</p>"),
                yesText: "Yes, Purge All",
                cancelText: "Cancel",
                options: new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small });

            if (confirmed != true)
            {
                return;
            }

            _purging = true;
            try
            {
                var result = await PurgeService.PurgeAllSoftDeletedRecordsAsync();
                var totalPurged = result.Values.Sum();

                if (totalPurged > 0)
                {
                    Snackbar.Add($"Successfully purged {totalPurged} records", Severity.Success);
                    Logger.LogInformation("Purged {TotalPurged} soft-deleted records. Details: {Details}", totalPurged, result);
                }
                else
                {
                    Snackbar.Add("No records were purged", Severity.Info);
                }

                // Refresh the counts
                await RefreshCountsAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error purging records: {ex.Message}", Severity.Error);
                Logger.LogError(ex, "Error purging soft-deleted records");
            }
            finally
            {
                _purging = false;
            }
        }

        private async Task ManualLoad()
        {
            Snackbar.Add("Manually loading...", Severity.Info);

            var newAssets = await AssetImportService.ImportAssets();

            Snackbar.Add($"Successfully loaded {newAssets} new Assets", Severity.Success);
        }

        private async Task RebuildThumbs()
        {
            Snackbar.Add("Rebuilding...", Severity.Info);

            var thumbCount = await ThumbnailService.RebuildAll();

            Snackbar.Add($"Successfully rebuilt {thumbCount} thumbnails", Severity.Success);
        }
    }
}
