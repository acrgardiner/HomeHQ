using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Category;

public partial class EditCategory
{
    [CascadingParameter] public required IMudDialogInstance Dialog { get; set; }

    [Parameter] public required HomeHQ.Entities.Category Entity { get; set; }
    [Parameter] public required string Title { get; set; }

    private MudForm _form;
    private HomeHQ.Entities.Category _model = new();

    protected override void OnInitialized()
    {
        _model = Entity;
    }

    private void Cancel() => Dialog.Cancel();

    private async Task Submit()
    {
        await _form.ValidateAsync();
        if (_form.IsValid)
        {
            try
            {
                await CategoryService.UpdateAsync(_model);
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
