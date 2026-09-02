using HomeHQ.DTOs;
using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.Pages.Admin;
using HomeHQ.Mobile.Pages.Setup;
using HomeHQ.Mobile.Services;
using HomeHQ.Mobile.ViewModels;
using HomeHQ.Mobile.ViewModels.Admin;
using HomeHQ.Mobile.ViewModels.Setup;
using MauiIcons.Material.Outlined;
using MauiNativePdfView;
using Microsoft.Extensions.Logging;

namespace HomeHQ.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMaterialOutlinedMauiIcons()
            .UseMauiNativePdfView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services (order matters - SettingsService first)
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AuthService>();

        // Handlers
        builder.Services.AddTransient<AuthDelegatingHandler>();
        builder.Services.AddTransient<CorrelationDelegatingHandler>();

        // Keep ApiClient as simple singleton for now (no HttpClientFactory on some MAUI platforms)
        builder.Services.AddSingleton<ApiClient>();
        // Reference-data cache services (one singleton per entity type)
        builder.Services.AddSingleton(sp =>
            new CacheService<CategoryDto>(sp.GetRequiredService<ApiClient>(), "api/categories"));
        builder.Services.AddSingleton(sp =>
            new CacheService<AttachmentTypeDto>(sp.GetRequiredService<ApiClient>(), "api/attachmenttypes"));
        builder.Services.AddSingleton(sp =>
            new CacheService<WarrantyTypeDto>(sp.GetRequiredService<ApiClient>(), "api/warrantytypes"));
        builder.Services.AddSingleton<AttachmentViewerNavigation>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<AssetsViewModel>();
        builder.Services.AddTransient<AssetDetailViewModel>();
        builder.Services.AddTransient<AssetEditViewModel>();
        builder.Services.AddTransient<AttachmentViewerViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<WarrantyTypesViewModel>();
        builder.Services.AddTransient<AttachmentTypesViewModel>();
        builder.Services.AddTransient<SetupEditViewModel>();
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<UsersViewModel>();
        builder.Services.AddTransient<UserEditViewModel>();
        builder.Services.AddTransient<MaintenanceViewModel>();
        builder.Services.AddTransient<LogsViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<AssetsPage>();
        builder.Services.AddTransient<AssetDetailPage>();
        builder.Services.AddTransient<AssetEditPage>();
        builder.Services.AddTransient<AttachmentViewerPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<WarrantyTypesPage>();
        builder.Services.AddTransient<AttachmentTypesPage>();
        builder.Services.AddTransient<SetupEditPage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<UserEditPage>();
        builder.Services.AddTransient<MaintenancePage>();
        builder.Services.AddTransient<LogsPage>();

        //#if DEBUG
        //        builder.Logging.AddDebug();
        //#endif

        return builder.Build();
    }
}
