using HomeHQ.Components.Entities.Common;
using HomeHQ.Entities;
using HomeHQ.Server.Components.Entities.Common;
using MudBlazor;
using System.Linq.Expressions;

namespace HomeHQ.Server.Components.Entities.Attachments;

public partial class ListAttachment
{
    private List<EntityAction<Attachment>> AttachmentActions => new()
{
    new EntityAction<Attachment>
    {
        Icon = Icons.Material.Filled.Image,
        Tooltip = "View",
        Action = async (attachment) => await ShowGalleryAsync(attachment)
    }
};

    private Task ShowGalleryAsync(Attachment attachment)
    {
        var param = new DialogParameters<ViewImage>()
    {
        { x => x.Attachment, attachment }
    };
        return DialogService.ShowAsync<ViewImage>("View Attachment", param);
    }

    List<Expression<Func<Attachment, object>>>? Includes => new List<Expression<Func<Attachment, object>>>
{
    x => x.AttachmentType
};
}
