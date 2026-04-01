using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Category;

public partial class CreateCategory
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    private MudForm _form = default!;
    private HomeHQ.Entities.Category _model = new();

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Submit()
    {
        await _form.Validate();
        if (_form.IsValid)
        {
            try
            {
                await CategoryService.AddAsync(_model);
                Snackbar.Add("Category created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating Category");
                Snackbar.Add("Error creating Category", Severity.Error);
            }
        }
    }
}
