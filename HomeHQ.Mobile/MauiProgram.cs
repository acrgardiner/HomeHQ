using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Services;
using HomeHQ.Mobile.ViewModels;
using MauiIcons.Material.Outlined;
using Microsoft.Extensions.Logging;

namespace HomeHQ.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseMaterialOutlinedMauiIcons();

        // Register Services (order matters - SettingsService first)
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<CategoryService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<AssetsViewModel>();
        builder.Services.AddTransient<AssetDetailViewModel>();
        builder.Services.AddTransient<AttachmentViewerViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<AssetsPage>();
        builder.Services.AddTransient<AssetDetailPage>();
        builder.Services.AddTransient<AttachmentViewerPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<DashboardPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
