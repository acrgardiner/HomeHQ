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

    private MudForm _form = default!;
    private readonly EditUserForm _model = new();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _model.UserName = User.UserName;

        try
        {
            var roles = await UserService.GetUserRolesAsync(User.Id);
            _model.IsAdmin = roles.Contains(Roles.Admin.ToString(), StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _loading = false;
        }
    }

    private class EditUserForm
    {
        [Required]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 256 characters.")]
        public string? UserName { get; set; }

        public bool IsAdmin { get; set; }
    }

    private void Cancel() => Dialog.Cancel();

    private async Task Submit()
    {
        await _form.ValidateAsync();
        if (!_form.IsValid || string.IsNullOrWhiteSpace(_model.UserName))
        {
            return;
        }

        try
        {
            var roles = new List<Roles> { Roles.Basic };
            if (_model.IsAdmin)
            {
                roles.Add(Roles.Admin);
            }

            var currentRoles = await UserService.GetUserRolesAsync(User.Id);
            if (currentRoles.Contains(Roles.SysAdmin.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                roles.Add(Roles.SysAdmin);
            }

            if (await UserService.UpdateUserAsync(User.Id, _model.UserName, roles))
            {
                Snackbar.Add("User updated successfully", Severity.Success);
                Dialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add("User update failed", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user");
            Snackbar.Add("Error updating user", Severity.Error);
        }
    }
}
