using HomeHQ.Application.Assets;
using HomeHQ.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomeHQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHomeHQApplication(this IServiceCollection services)
    {
        services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));
        services.AddScoped<IGetAssetDashboardStatsHandler, GetAssetDashboardStatsHandler>();
        return services;
    }
}
