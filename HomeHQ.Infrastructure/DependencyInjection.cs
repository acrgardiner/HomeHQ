using HomeHQ.Data;
using HomeHQ.Infrastructure.Persistence;
using HomeHQ.Infrastructure.Polymorphism;
using HomeHQ.Polymorphism;
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
        services.AddScoped<AuditingSaveChangesInterceptor>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IPolymorphicChildStore, EfPolymorphicChildStore>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlite(
                connectionString,
                sqlite => sqlite.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
