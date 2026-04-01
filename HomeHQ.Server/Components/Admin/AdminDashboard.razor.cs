using HomeHQ.Components.Entities.Common;
using HomeHQ.Entities;
using HomeHQ.Server.Components.Entities.Common;
using MudBlazor;
using System.Linq.Expressions;

namespace HomeHQ.Server.Components.Admin
{
    public partial class AdminDashboard
    {
        private bool _loading;

        private int _usercount;
        private int _attachmentcount;
        private List<Attachment> _largeAttach = new();

        private List<Asset> _warrantyExpirigSoon = new();
        private float _totalAttachmentSize;

        private double[] _pendingPurgeData = Array.Empty<double>();
        private string[] _pendingPurgeLabels = Array.Empty<string>();

        protected override async Task OnInitializedAsync()
        {
            await RefreshDataAsync();
        }

        private async Task RefreshDataAsync()
        {
            _loading = true;
            try
            {
                List<Task> tasks = new List<Task>
            {
                UpdateUserCountAsync(),
                UpdateAttachmentCountAsync(),
                GetLargeFiles(),
                GetTotalAttachmentSize(),
                GetWarrantiesExpiringSoon(),
                GetDeletedPendingPurge()
            };
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error loading dashboard data", Severity.Error);
                Logger.LogError(ex, $"Error loading dashboard data");
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task UpdateUserCountAsync()
        {
            _usercount = await UserService.GetUserCountAsync();
        }

        private async Task UpdateAttachmentCountAsync()
        {
            _attachmentcount = await AttachmentService.CountAsync();
        }

        private async Task GetLargeFiles()
        {
            _largeAttach = (await AttachmentService.GetAsync(
                orderby: attach => attach.FileSize,
                descending: true,
                take: 10,
                includes: new() { x => x.Parent }
            )).ToList();
        }

        private async Task GetWarrantiesExpiringSoon()
        {
            var dateFrom = DateTime.UtcNow;
            var dateTo = DateTime.UtcNow.AddYears(1);
            _warrantyExpirigSoon = (await AssetService.GetAsync(filter: x => x.WarrantyExpiration.HasValue && x.WarrantyExpiration > dateFrom && x.WarrantyExpiration < dateTo,
                    orderby: attach => attach.WarrantyExpiration,
                    descending: true,
                    take: 10,
                    includes: new List<Expression<Func<Asset, object>>> { x => x.Category, x => x.WarrantyType }
            )).ToList();
        }

        private async Task GetTotalAttachmentSize()
        {
            _totalAttachmentSize = await AttachmentService.SumAsync(selector: asset => asset.FileSize);
        }

        private async Task GetDeletedPendingPurge()
        {
            try
            {
                var purgeRecords = await PurgeService.GetSoftDeletedCountAsync();

                _pendingPurgeLabels = purgeRecords.Keys.ToArray();
                _pendingPurgeData = purgeRecords.Values.Select(v => (double)v).ToArray();
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error loading pending purge data", Severity.Error);
                Logger.LogError(ex, $"Error loading pending purge data");
            }
        }

        private Task ShowGalleryAsync(Attachment attachment)
        {
            var param = new DialogParameters<ViewImage>()
        {
            { x => x.Attachment, attachment }
        };
            return DialogService.ShowAsync<ViewImage>("View Attachment", param);
        }

        private void NavigateToAsset(Guid assetId)
        {
            var state = NavigationStateHelper.CreateState("/admin/dashboard");
            var encodedState = NavigationStateHelper.Encode(state);
            var queryString = !string.IsNullOrWhiteSpace(encodedState) ? $"?returnState={Uri.EscapeDataString(encodedState)}" : "";
            Navigation.NavigateTo($"/assets/{assetId}{queryString}");
        }
    }
}
