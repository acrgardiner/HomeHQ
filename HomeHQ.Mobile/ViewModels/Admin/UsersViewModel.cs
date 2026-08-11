using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows.Input;
using HomeHQ.DTOs;
using HomeHQ.Helpers;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels.Admin;

public class UsersViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private List<UserListItem> _allUsers = [];

    public ObservableCollection<UserListItem> Users { get; } = [];

    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = string.Empty;

    public bool IsLoading
    {
        get;
        set
        {
            SetProperty(ref field, value);
            UpdateVisibility();
        }
    }

    public bool IsRefreshing
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
            UpdateVisibility();
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool ShowEmptyState
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowList
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand DeleteCommand { get; }

    public UsersViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        RefreshCommand = new Command(async () => await LoadAsync(forceRefresh: true));
        AddCommand = new Command(async () => await NavigateToEditAsync(null));
        EditCommand = new Command<UserListItem>(async user => await NavigateToEditAsync(user));
        ResetPasswordCommand = new Command<UserListItem>(async user => await ResetPasswordAsync(user));
        DeleteCommand = new Command<UserListItem>(async user => await DeleteAsync(user));
    }

    public async Task LoadAsync(bool forceRefresh = false)
    {
        if (IsLoading && !forceRefresh)
        {
            return;
        }

        try
        {
            IsLoading = !forceRefresh;
            IsRefreshing = forceRefresh;
            ErrorMessage = null;

            var response = await _apiClient.GetAsync("api/users");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                ErrorMessage = apiResponse?.Errors.FirstOrDefault() ?? "Failed to load users.";
                return;
            }

            _allUsers = apiResponse.Data
                .Select(u => new UserListItem
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    LastLogin = u.LastLogin,
                    LastLoginDisplay = u.LastLogin.HasValue
                        ? Formatter.FormatDateTime(u.LastLogin.Value.ToLocalTime())
                        : "Never",
                    RolesDisplay = u.Roles.Count > 0 ? string.Join(", ", u.Roles) : "Basic",
                    IsAdmin = u.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase)
                })
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ApplyFilter();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Connection error: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            UpdateVisibility();
        }
    }

    private void ApplyFilter()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsers
            : _allUsers.Where(u =>
                u.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Users.Clear();
        foreach (var user in results)
        {
            Users.Add(user);
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        ShowEmptyState = !IsLoading && !HasError && Users.Count == 0;
        ShowList = !IsLoading && !HasError && Users.Count > 0;
    }

    private async Task NavigateToEditAsync(UserListItem? user)
    {
        var parameters = new Dictionary<string, object>
        {
            { "userId", user?.Id ?? string.Empty },
            { "userName", user?.UserName ?? string.Empty },
            { "isAdmin", (user?.IsAdmin == true).ToString() }
        };

        await SafeExecuteAsync(
            () => Shell.Current.GoToAsync("UserEdit", parameters),
            onError: ex => ErrorMessage = $"Navigation failed: {ex.Message}");
    }

    private async Task ResetPasswordAsync(UserListItem? user)
    {
        if (user is null)
        {
            return;
        }

        var password = await Shell.Current.DisplayPromptAsync(
            "Reset Password",
            $"Enter a new password for {user.UserName} (min 6 characters):",
            "Reset",
            "Cancel",
            maxLength: 100,
            keyboard: Keyboard.Default);

        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (password.Length < 6)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Password must be at least 6 characters.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayPromptAsync(
            "Confirm Password",
            "Re-enter the new password:",
            "Confirm",
            "Cancel",
            maxLength: 100,
            keyboard: Keyboard.Default);

        if (confirm != password)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var response = await _apiClient.PostAsJsonAsync(
                $"api/users/{user.Id}/reset-password",
                new ResetPasswordRequest(password));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            if (apiResponse?.Success == true)
            {
                await Shell.Current.DisplayAlertAsync("Success", $"Password reset for {user.UserName}.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    apiResponse?.Errors.FirstOrDefault() ?? "Failed to reset password.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync(UserListItem? user)
    {
        if (user is null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete User",
            $"Are you sure you want to delete '{user.UserName}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        // Matches Blazor Admin: delete confirmation is shown but delete is not implemented yet.
        await Shell.Current.DisplayAlertAsync(
            "Not Available",
            "User deletion is not yet implemented on the server.",
            "OK");
    }
}
