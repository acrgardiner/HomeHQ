using HomeHQ.Data;
using HomeHQ.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HomeHQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHomeHQInfrastructure(
        this IServiceCollection services,
        string connectionString = "Data Source=appdata/db/app.db")
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(
                connectionString,
                sqlite => sqlite.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
