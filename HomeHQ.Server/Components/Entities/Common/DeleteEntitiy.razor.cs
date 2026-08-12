using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Common;

public partial class DeleteEntitiy
{
    [CascadingParameter] public required IMudDialogInstance MudDialog { get; set; }
    [Parameter] public required Guid Id { get; set; }
    [Parameter] public required string Message { get; set; }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(true));
}
