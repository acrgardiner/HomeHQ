using HomeHQ.Identity;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HomeHQ.Server.Components.Admin.Users
{
    public partial class ResetPasswordDialog
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter] public ApplicationUser? User { get; set; }

        private readonly ResetPasswordModel _model = new();
        private bool _isResetting = false;

        // Password visibility toggles
        private bool _passwordVisible = false;
        private InputType _passwordInput = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        private bool _confirmPasswordVisible = false;
        private InputType _confirmPasswordInput = InputType.Password;
        private string _confirmPasswordInputIcon = Icons.Material.Filled.VisibilityOff;

        private void TogglePasswordVisibility()
        {
            _passwordVisible = !_passwordVisible;
            if (_passwordVisible)
            {
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }
            else
            {
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            StateHasChanged();
        }

        private void ToggleConfirmPasswordVisibility()
        {
            _confirmPasswordVisible = !_confirmPasswordVisible;
            if (_confirmPasswordVisible)
            {
                _confirmPasswordInputIcon = Icons.Material.Filled.Visibility;
                _confirmPasswordInput = InputType.Text;
            }
            else
            {
                _confirmPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                _confirmPasswordInput = InputType.Password;
            }
            StateHasChanged();
        }

        private bool IsFormValid()
        {
            return !string.IsNullOrWhiteSpace(_model.NewPassword) &&
                   !string.IsNullOrWhiteSpace(_model.ConfirmPassword) &&
                   _model.NewPassword == _model.ConfirmPassword &&
                   _model.NewPassword.Length >= 6;
        }

        private async Task ResetPassword()
        {
            if (User == null || !IsFormValid())
                return;

            _isResetting = true;
            try
            {
                var success = await UserService.ResetPasswordAsync(User.Id, _model.NewPassword);

                if (success)
                {
                    Snackbar.Add($"Password reset successfully for {User.UserName}", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Failed to reset password. Please try again.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error resetting password: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isResetting = false;
            }
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private sealed class ResetPasswordModel
        {
            [Required(ErrorMessage = "New password is required")]
            [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm the password")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}