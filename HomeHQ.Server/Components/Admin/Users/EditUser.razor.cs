using HomeHQ.Identity;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HomeHQ.Server.Components.Admin.Users;

public partial class EditUser
{
    [CascadingParameter] public required IMudDialogInstance Dialog { get; set; }

    [Parameter] public required ApplicationUser User { get; set; }
    [Parameter] public required string Title { get; set; }

    private MudForm _form;
    private readonly EditUserForm _model = new();

    protected override void OnInitialized()
    {
        _model.UserName = User.UserName;
    }

    private class EditUserForm
    {
        [Required]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 256 characters.")]
        public string? UserName { get; set; }
    }

    private void Cancel() => Dialog.Cancel();

    private async Task Submit()
    {
        await _form.ValidateAsync();
        if (_form.IsValid)
        {
            try
            {
                if (await UserService.UpdateUserName(User.Id, _model.UserName))
                {
                    Snackbar.Add("User updated successfully", Severity.Success);
                }
                else
                {
                    Snackbar.Add("User update failed", Severity.Error);
                }

                Dialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating user");
                Snackbar.Add("Error updating user", Severity.Error);
            }
        }
    }
}
