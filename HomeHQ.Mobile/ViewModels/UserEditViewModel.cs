using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

[QueryProperty(nameof(UserId), "userId")]
[QueryProperty(nameof(UserName), "userName")]
public class UserEditViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private bool _initialized;

    public string UserId
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsCreate));
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(ShowPasswordFields));
                OnPropertyChanged(nameof(ShowAdminToggle));
            }
        }
    } = string.Empty;

    public string UserName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string Password
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string ConfirmPassword
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public bool IsAdmin
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsCreate => string.IsNullOrWhiteSpace(UserId);
    public bool ShowPasswordFields => IsCreate;
    public bool ShowAdminToggle => IsCreate;
    public string PageTitle => IsCreate ? "Create User" : "Edit User";

    public bool IsLoading
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool IsSaving
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? ErrorMessage
    {
        get;
        set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(ShowContent));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool ShowContent => !IsLoading && !HasError;

    public ICommand SaveCommand { get; }
    public ICommand GoBackCommand { get; }

    public UserEditViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public Task InitializeAsync()
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        _initialized = true;
        IsLoading = false;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(UserName))
        {
            await Shell.Current.DisplayAlertAsync("Validation", "Username is required.", "OK");
            return;
        }

        if (IsCreate)
        {
            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                await Shell.Current.DisplayAlertAsync("Validation", "Password must be at least 6 characters.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Shell.Current.DisplayAlertAsync("Validation", "Passwords do not match.", "OK");
                return;
            }
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            if (IsCreate)
            {
                var response = await _apiClient.PostAsJsonAsync(
                    "api/users",
                    new CreateUserRequest(UserName.Trim(), Password, IsAdmin));

                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.Forbidden)
                {
                    await Shell.Current.GoToAsync("//Login");
                    return;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
                if (apiResponse?.Success != true)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Error",
                        apiResponse?.Errors.FirstOrDefault() ?? "Failed to create user.",
                        "OK");
                    return;
                }
            }
            else
            {
                var response = await _apiClient.PutAsJsonAsync(
                    $"api/users/{UserId}",
                    new UpdateUserRequest(UserName.Trim()));

                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.Forbidden)
                {
                    await Shell.Current.GoToAsync("//Login");
                    return;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
                if (apiResponse?.Success != true)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Error",
                        apiResponse?.Errors.FirstOrDefault() ?? "Failed to update user.",
                        "OK");
                    return;
                }
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
