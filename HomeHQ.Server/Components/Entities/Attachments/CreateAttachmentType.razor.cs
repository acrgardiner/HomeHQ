using HomeHQ.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Attachments;

public partial class CreateAttachmentType
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    private MudForm form = default!;
    private AttachmentType model = new();

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Submit()
    {
        await form.Validate();
        if (form.IsValid)
        {
            try
            {
                await AttachmentService.AddAsync(model);
                Snackbar.Add("Attachment Type created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating Attachment Type");
                Snackbar.Add("Error creating Attachment Type", Severity.Error);
            }
        }
    }
}
