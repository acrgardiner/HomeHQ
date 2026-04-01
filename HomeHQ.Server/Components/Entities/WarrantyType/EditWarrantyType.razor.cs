using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.WarrantyType;

public partial class EditWarrantyType
{
    [CascadingParameter] public required IMudDialogInstance Dialog { get; set; }

    [Parameter] public required HomeHQ.Entities.WarrantyType Entity { get; set; }
    [Parameter] public required string Title { get; set; }

    private MudForm _form;
    private HomeHQ.Entities.WarrantyType _model = new();

    protected override void OnInitialized()
    {
        _model = Entity;
    }

    private void Cancel() => Dialog.Cancel();

    private async Task Submit()
    {
        await _form.Validate();
        if (_form.IsValid)
        {
            try
            {
                await WarrantyService.UpdateAsync(_model);
                Snackbar.Add("updated successfully", Severity.Success);
                Dialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating");
                Snackbar.Add("Error updating", Severity.Error);
            }
        }
    }
}
