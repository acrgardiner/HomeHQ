using HomeHQ.Identity;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HomeHQ.Server.Components.Admin.Users
{
    public partial class CreateUser
    {
        [CascadingParameter] public required IMudDialogInstance MudDialog { get; set; } = default!;
        private readonly DialogOptions _dialogOptions = new() { FullWidth = true, MaxWidth = MaxWidth.Small };

        private NewUserForm newUser = new();

        // Password visibility toggles
        private bool _passwordVisible = false;
        private InputType _passwordInput = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        private bool _confirmPasswordVisible = false;
        private InputType _confirmPasswordInput = InputType.Password;
        private string _confirmPasswordInputIcon = Icons.Material.Filled.VisibilityOff;

        protected override async Task OnInitializedAsync()
        {
            await MudDialog.SetOptionsAsync(_dialogOptions);
        }

        private class NewUserForm
        {
            [Required]
            public string UserName { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = string.Empty;

            public bool IsAdmin { get; set; } = false;

            public NewUserForm()
            {
            }
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private async Task Submit()
        {
            try
            {
                List<Roles> userRoles = new() { Roles.Basic };

                if (newUser.IsAdmin)
                {
                    userRoles.Add(Roles.Admin);
                }
                ;

                await UserService.AddUser(newUser.UserName, newUser.Password, userRoles);
                Snackbar.Add("User created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating user");
                Snackbar.Add("Error creating user", Severity.Error);
            }
        }

        private void TogglePasswordVisibility()
        {
            if (_passwordVisible)
            {
                _passwordVisible = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            else
            {
                _passwordVisible = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }
        }

        private void ToggleConfirmPasswordVisibility()
        {
            if (_confirmPasswordVisible)
            {
                _confirmPasswordVisible = false;
                _confirmPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                _confirmPasswordInput = InputType.Password;
            }
            else
            {
                _confirmPasswordVisible = true;
                _confirmPasswordInputIcon = Icons.Material.Filled.Visibility;
                _confirmPasswordInput = InputType.Text;
            }
        }
    }
}