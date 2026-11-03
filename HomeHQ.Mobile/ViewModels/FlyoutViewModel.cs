using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public sealed class FlyoutViewModel : BaseViewModel
{
#pragma warning disable IDE0032
    private readonly AuthService _authService;
    private bool _isSetupExpanded;
    private bool _isAdminExpanded;
    private bool _isAdmin;

    public FlyoutViewModel(AuthService authService)
    {
        _authService = authService;
        NavigateCommand = new Command<string>(async route => await NavigateAsync(route));
    }

    public bool IsSetupExpanded
    {
        get => _isSetupExpanded;
        set => SetProperty(ref _isSetupExpanded, value);
    }

    public bool IsAdminExpanded
    {
        get => _isAdminExpanded;
        set => SetProperty(ref _isAdminExpanded, value);
    }

    public bool IsAdmin
    {
        get => _isAdmin;
        private set
        {
            if (SetProperty(ref _isAdmin, value) && !value)
            {
                IsAdminExpanded = false;
            }
        }
    }

    public Command<string> NavigateCommand { get; }

    public async Task RefreshAdminVisibilityAsync()
    {
        var isAdmin = false;

        try
        {
            if (await _authService.IsAuthenticatedAsync())
            {
                var roles = await _authService.GetRolesAsync();
                if (roles.Count == 0)
                {
                    await _authService.RefreshUserInfoAsync();
                }

                isAdmin = await _authService.IsAdminAsync();
            }
        }
        catch
        {
            isAdmin = false;
        }

        IsAdmin = isAdmin;
    }

    public void Reset()
    {
        IsAdmin = false;
        IsSetupExpanded = false;
    }

    private static async Task NavigateAsync(string? route)
    {
        if (!string.IsNullOrWhiteSpace(route))
        {
            await Shell.Current.GoToAsync($"//{route}");
            Shell.Current.FlyoutIsPresented = false;
        }
    }
}
