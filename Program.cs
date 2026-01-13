using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Template.Components.Account;
using HomeHQ.Components;
using HomeHQ.Components.Account;
using HomeHQ.Data;
using HomeHQ.Entities;
using HomeHQ.FileStorage;
using HomeHQ.Identity;
using HomeHQ.Logging;
using HomeHQ.Repositories;
using HomeHQ.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(formatter: new CustomJsonFormatter(),
        path: "appdata/logs/log-.json",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true
    )
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

        config.SnackbarConfiguration.PreventDuplicates = false;
        config.SnackbarConfiguration.NewestOnTop = false;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 2000;
        config.SnackbarConfiguration.HideTransitionDuration = 500;
        config.SnackbarConfiguration.ShowTransitionDuration = 500;
        config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    });

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddHubOptions(options =>
        {
            options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
        });

    // Add services to the container.
    //builder.Services.AddRazorComponents()
    //    .AddInteractiveServerComponents()
    //    .AddHubOptions(options =>
    //    {
    //        options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    //        // Extended timeouts for camera operations - give users plenty of time
    //        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    //        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    //        options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    //    });
    //
    //// Configure circuit options for better handling of disconnections and long-running JS operations
    //builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
    //{
    //    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    //    options.DisconnectedCircuitMaxRetained = 100;
    //    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
    //});

    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly",
             policy => policy.RequireRole("Admin"));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    builder.Services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<IAssetImportService, AssetImportService>();
    builder.Services.AddScoped<IThumbnailService, ThumbnailService>();
    builder.Services.AddScoped<IPurgeService, PurgeService>();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite("Data Source=appdata/db/app.db");
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    );

    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
            options.User.RequireUniqueEmail = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

    Directory.CreateDirectory("appdata");
    Directory.CreateDirectory(Path.Combine("appdata", "logs"));
    Directory.CreateDirectory(Path.Combine("appdata", "imports"));
    Directory.CreateDirectory(Path.Combine("appdata", "attachments"));
    Directory.CreateDirectory(Path.Combine("appdata", "thumbs"));
    Directory.CreateDirectory(Path.Combine("appdata", "db"));

    builder.Services.AddControllers(options =>
    {
        options.ModelBinderProviders.Insert(0, new ModelBinderProvider());
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        
        // Configure trusted proxies from environment variable
        var trustedProxiesEnv = builder.Configuration["TRUSTED_PROXIES"] ?? Environment.GetEnvironmentVariable("TRUSTED_PROXIES");
        if (!string.IsNullOrWhiteSpace(trustedProxiesEnv))
        {
            var trustedProxies = trustedProxiesEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim())
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();

            foreach (var proxyIp in trustedProxies)
            {
                if (System.Net.IPAddress.TryParse(proxyIp, out var ipAddress))
                {
                    options.KnownProxies.Add(ipAddress);
                    Log.Information("Added trusted proxy: {ProxyIP}", proxyIp);
                }
                else
                {
                    Log.Warning("Invalid proxy IP address in TRUSTED_PROXIES: {ProxyIP}", proxyIp);
                }
            }
            
            Log.Information("Configured {Count} trusted proxies", options.KnownProxies.Count);
        }
        else
        {
            Log.Information("No TRUSTED_PROXIES environment variable found - no proxy restrictions applied");
        }
    });

    var app = builder.Build();

    //Prepare Database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();

            context.ShowDeleted = true; // Show deleted items if any

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            List<Task> tasks = new List<Task>();
            tasks.Add(ContextSeed.SeedRolesAsync(userManager, roleManager));
            tasks.Add(ContextSeed.SeedSysAdminAsync(userManager, roleManager));

            tasks.Add(ContextSeed.SeedCategories(context));
            tasks.Add(ContextSeed.SeedAttachmentTypes(context));
            tasks.Add(ContextSeed.SeedWarrantyTypes(context));

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "An error occurred seeding the DB.");
        }
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseForwardedHeaders();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHttpsRedirection();
    app.MapControllers();
    app.UseStaticFiles();

    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Add additional endpoints required by the Identity /Account Razor components.
    app.MapAdditionalIdentityEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
