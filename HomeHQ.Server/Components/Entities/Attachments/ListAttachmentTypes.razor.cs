using HomeHQ.Entities;
using HomeHQ.Server.Components.Entities.Common;

namespace HomeHQ.Server.Components.Entities.Attachments;

public partial class ListAttachmentTypes
{
    private EntityList<AttachmentType>? entityList;

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
    }

    protected override async Task OnParametersSetAsync()
    {
        await entityList?.ReloadData();
    }

    private async Task OpenCreate()
    {
        await DialogService.ShowAsync<CreateAttachmentType>("Add New Attachment Type");
    }
}
