using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.WarrantyType;

public partial class CreateWarrantyType
{
    [CascadingParameter] public required IMudDialogInstance MudDialog { get; set; }
    private MudForm _form;
    private readonly HomeHQ.Entities.WarrantyType _model;

    public CreateWarrantyType()
    {
        _model = new HomeHQ.Entities.WarrantyType();
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Submit()
    {
        await _form.ValidateAsync();
        if (_form.IsValid)
        {
            try
            {
                await WarrantyService.AddAsync(_model);
                Snackbar.Add("Warranty Type created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating Warranty Type");
                Snackbar.Add("Error creating Warranty Type", Severity.Error);
            }
        }
    }
}
