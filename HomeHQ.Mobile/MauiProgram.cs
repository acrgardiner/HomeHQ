using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Services;
using HomeHQ.Mobile.ViewModels;
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
                fonts.AddFont("fontawesome.ttf", "FontAwesomeRegular");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIconsRegular");
                fonts.AddFont("MaterialSymbolsOutlined.ttf", "MSO"); // MSO = Material Symbols Outlined
            });

        // Register Services (order matters - SettingsService first)
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<CategoryService>();

        builder.Services.AddSingleton<SharedImageService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<AssetsViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<NewAssetViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<AssetsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<NewAssetPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
