using HomeHQ.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq.Expressions;

namespace HomeHQ.Server.Components.Entities.Common;

public partial class AttachmentList_view
{
    [Parameter] public Guid? ParentId { get; set; }

    private List<Attachment> _attachments { get; set; } = new();

    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadAttachments();
    }

    private async Task ReloadData()
    {
        await LoadAttachments();
    }

    private async Task LoadAttachments()
    {
        try
        {
            _loading = true;
            _attachments = (await AttachmentService.GetAsync(
                filter: x => x.ParentId == ParentId,
                orderby: x => x.CreatedOn,
                descending: true,
                take: 10,
                includes: new List<Expression<Func<Attachment, object>>> { x => x.AttachmentType }
            )).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading attachments for ParentId {ParentId}", ParentId);
            Snackbar.Add("Failed to load", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private Task<IDialogReference> ShowGalleryAsync(Attachment attachment)
    {
        var param = new DialogParameters<ViewImage>()
        {
            { x => x.Attachment, attachment }
        };
        return DialogService.ShowAsync<ViewImage>("View Attachment", param);
    }
}
