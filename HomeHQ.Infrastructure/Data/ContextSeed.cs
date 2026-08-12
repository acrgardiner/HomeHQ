using Microsoft.AspNetCore.Identity;
using HomeHQ.Entities;
using HomeHQ.Identity;
using Microsoft.Extensions.Logging;

namespace HomeHQ.Data;

public class ContextSeed
{
    private readonly ILogger<ContextSeed> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public ContextSeed(ILogger<ContextSeed> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Seeds the database with a predefined set of data
    /// </summary>
    /// <remarks>
    /// System critical data, including roles, a system administrator user, categories, attachment types, and warranty types.
    /// </remarks>
    /// <returns>A task that represents the asynchronous seeding operation</returns>
    public async Task SeedAsync()
    {
        var result = await Task.WhenAll(
             SeedRolesAsync(),
             SeedSysAdminAsync(),
             SeedCategories(),
             SeedAttachmentTypes(),
             SeedWarrantyTypes()
        );

        if (result.Any(x => x)) //if any changes are made
        {
            _logger.LogInformation("Seeding completed with changes. Saving to database.");
            await _context.SaveChangesAsync();
        }
    }

    private async Task<bool> SeedRolesAsync()
    {
        if (_roleManager.Roles.Any())
        {
            _logger.LogInformation("Roles already exist. Skipping role seeding.");
            return false;
        }

        _logger.LogInformation("Seeding user roles into the database.");

        //Seed Roles
        await Task.WhenAll(
             _roleManager.CreateAsync(new ApplicationRole(Roles.SysAdmin.ToString())),
             _roleManager.CreateAsync(new ApplicationRole(Roles.Admin.ToString())),
             _roleManager.CreateAsync(new ApplicationRole(Roles.Basic.ToString()))
        );

        return true;
    }

    private async Task<bool> SeedSysAdminAsync()
    {
        if (_userManager.Users.Any())
        {
            _logger.LogInformation("Users already exist. Skipping system administrator seeding.");
            return false;
        }

        var sysAdminUser = new ApplicationUser
        {
            UserName = "sysadmin"
        };

        if (_userManager.Users.Any(u => u.Id == sysAdminUser.Id))
        {
            return false;
        }

        var user = await _userManager.FindByNameAsync(sysAdminUser.UserName);
        if (user != null)
        {
            return false;
        }

        _logger.LogInformation("Creating system administrator user.");

        await _userManager.CreateAsync(sysAdminUser, "Password123!");
        await Task.WhenAll(
            _userManager.AddToRoleAsync(sysAdminUser, Roles.Basic.ToString()),
            _userManager.AddToRoleAsync(sysAdminUser, Roles.Admin.ToString()),
            _userManager.AddToRoleAsync(sysAdminUser, Roles.SysAdmin.ToString())
        );

        return true;
    }

    private async Task<bool> SeedCategories()
    {
        if (_context.Categories.Any())
        {
            _logger.LogInformation("Categories already exist. Skipping category seeding.");
            return false;
        }

        _logger.LogInformation("Seeding initial categories into the database.");

        await _context.Categories.AddRangeAsync(
            new Category { Id = new Guid(), Icon = "📺", Name = "Electronics" },
            new Category { Id = new Guid(), Icon = "💍", Name = "Jewelery" },
            new Category { Id = new Guid(), Icon = "👚", Name = "Apparel" },
            new Category { Id = new Guid(), Icon = "🛠", Name = "Garage" },
            new Category { Id = new Guid(), Icon = "🌲", Name = "Garden" },
            new Category { Id = new Guid(), Icon = "🛋", Name = "Lounge" },
            new Category { Id = new Guid(), Icon = "🧺", Name = "Laundry" },
            new Category { Id = new Guid(), Icon = "🍳", Name = "Kitchen" },
            new Category { Id = new Guid(), Icon = "🛏️", Name = "Bedroom" },
            new Category { Id = new Guid(), Icon = "🚗", Name = "Automotive" },
            new Category { Id = new Guid(), Icon = "❓", Name = "Misc" }
        );

        return true;
    }

    private async Task<bool> SeedAttachmentTypes()
    {
        if (_context.AttachmentTypes.Any())
        {
            _logger.LogInformation("Attachment types already exist. Skipping attachment type seeding.");
            return false;
        }

        _logger.LogInformation("Seeding initial attachment types into the database.");

        await _context.AttachmentTypes.AddRangeAsync(
            new AttachmentType { Id = new Guid(), Name = "Receipt", Default = true }
            , new AttachmentType { Id = new Guid(), Name = "User Manual" }
            , new AttachmentType { Id = new Guid(), Name = "Photo" }
        );

        return true;
    }

    private async Task<bool> SeedWarrantyTypes()
    {
        if (_context.WarrantyTypes.Any())
        {
            _logger.LogInformation("Warranty types already exist. Skipping warranty type seeding.");
            return false;
        }

        _logger.LogInformation("Seeding initial warranty types into the database.");

        await _context.WarrantyTypes.AddRangeAsync(
            new WarrantyType { Id = new Guid(), Name = "Custom", Days = 1, Months = null, Years = null },
            new WarrantyType { Id = new Guid(), Name = "6 Month", Days = null, Months = 6, Years = null },
            new WarrantyType { Id = new Guid(), Name = "12 Month", Days = null, Months = 12, Years = null, Default = true },
            new WarrantyType { Id = new Guid(), Name = "2 Year", Days = null, Months = null, Years = 2 },
            new WarrantyType { Id = new Guid(), Name = "5 Year", Days = null, Months = null, Years = 5 },
            new WarrantyType { Id = new Guid(), Name = "10 Year", Days = null, Months = null, Years = 10 },
            new WarrantyType { Id = new Guid(), Name = "Lifetime", Days = null, Months = null, Years = 999 }
        );

        return true;
    }

}
