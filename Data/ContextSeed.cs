using Microsoft.AspNetCore.Identity;
using projectaardvarkx2.Entities;
using projectaardvarkx2.Identity;
using System;

namespace projectaardvarkx2.Data;

public class ContextSeed
{

    public static async Task SeedRolesAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        if (roleManager.Roles.Any())
        {
            return;   // DB has been seeded
        }

        //Seed Roles
        await roleManager.CreateAsync(new ApplicationRole(Roles.SysAdmin.ToString()));
        await roleManager.CreateAsync(new ApplicationRole(Roles.Admin.ToString()));
        await roleManager.CreateAsync(new ApplicationRole(Roles.Basic.ToString()));
    }

    public static async Task SeedSysAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        if (userManager.Users.Any())
        {
            return;     // DB has been seeded
        }

        //Seed System Admin User
        var sysAdminUser = new ApplicationUser
        {
            UserName = "sysadmin@email.com",
            Email = "sysadmin@email.com",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };
        if (userManager.Users.All(u => u.Id != sysAdminUser.Id))
        {
            var user = await userManager.FindByNameAsync(sysAdminUser.UserName);
            if (user == null)
            {
                await userManager.CreateAsync(sysAdminUser, "Password123!");
                await userManager.AddToRoleAsync(sysAdminUser, Roles.Basic.ToString());
                await userManager.AddToRoleAsync(sysAdminUser, Roles.Admin.ToString());
                await userManager.AddToRoleAsync(sysAdminUser, Roles.SysAdmin.ToString());
            }

        }
    }

    public static async Task SeedCategories(ApplicationDbContext context)
    {
        // Seed Categories only if they don't exist
        if (context.Categories.Any())
            return;

        await context.Categories.AddRangeAsync(
            new Category { Id = new Guid(), Icon = "📺", Name = "Electronics" },
            new Category { Id = new Guid(), Icon = "💍", Name = "Jewelry" },
            new Category { Id = new Guid(), Icon = "🛠", Name = "Tools" },
            new Category { Id = new Guid(), Icon = "🛋", Name = "Furniture" },
            new Category { Id = new Guid(), Icon = "🚗", Name = "Vehicles" }
        );

        context.SaveChanges();
    }

    public static async Task SeedAttachmentTypes(ApplicationDbContext context)
    {
        // Seed Categories only if they don't exist
        if (context.AttachmentTypes.Any())
            return;

        await context.AttachmentTypes.AddRangeAsync(
            new AttachmentType { Id = new Guid(), Name = "Receipt", Default = true }
            , new AttachmentType { Id = new Guid(), Name = "Manual" }
        );

        context.SaveChanges();
    }

    public static async Task SeedWarrantyTypes(ApplicationDbContext context)
    {
        // Seed Categories only if they don't exist
        if (context.WarrantyTypes.Any())
            return;

        await context.WarrantyTypes.AddRangeAsync(
            new WarrantyType { Id = new Guid(), Name = "6 Month", Days = null, Months = 6, Years = null },
            new WarrantyType { Id = new Guid(), Name = "12 Month", Days = null, Months = 12, Years = null, Default = true },
            new WarrantyType { Id = new Guid(), Name = "2 Year", Days = null, Months = null, Years = 2 },
            new WarrantyType { Id = new Guid(), Name = "5 Year", Days = null, Months = null, Years = 5 },
            new WarrantyType { Id = new Guid(), Name = "10 Year", Days = null, Months = null, Years = 10 },
            new WarrantyType { Id = new Guid(), Name = "Lifetime", Days = null, Months = null, Years = 999 }
        );

        context.SaveChanges();
    }

}
