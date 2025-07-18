using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projectaardvarkx2.Components;
using projectaardvarkx2.Components.Account;
using projectaardvarkx2.Data;
using projectaardvarkx2.Identity;
using projectaardvarkx2.Services;
using projectaardvarkx2.Logging;
using Serilog;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using projectaardvarkx2.Repositories;
using projectaardvarkx2.Entities;
using MudBlazor.Template.Components.Account;
using projectaardvarkx2.FileStorage;
using Microsoft.Extensions.FileProviders;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(formatter: new CustomJsonFormatter(),
        path: "logs/log-.json",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true
    )
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddMudServices();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

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

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite("Data Source=appdata/app.db");
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    );

    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

    Directory.CreateDirectory("appdata");
    Directory.CreateDirectory("logs");

    builder.Services.AddControllers();

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

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHttpsRedirection();
    app.MapControllers();
    app.UseStaticFiles();
    //app.UseStaticFiles(new StaticFileOptions
    //{
    //    FileProvider = new PhysicalFileProvider(
    //    Path.Combine(app.Environment.ContentRootPath, "appdata/uploads")), // Replace "MySecuredImages" with your folder name
    //    RequestPath = "/images" // Specify the URL path to access these images
    //});

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
