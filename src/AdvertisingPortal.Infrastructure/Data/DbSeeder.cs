using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AdvertisingPortal.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        foreach (var role in new[] { RoleNames.Admin, RoleNames.Advertiser })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@localads.com";
        var adminPassword = configuration["SeedAdmin:Password"] ?? "Admin@12345";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Portal Administrator"
            };

            var created = await userManager.CreateAsync(admin, adminPassword);
            if (created.Succeeded)
                await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        }

        if (!await db.Localities.AnyAsync())
        {
            db.Localities.Add(new Locality
            {
                Name = SeedConstants.DefaultLocalityName,
                Slug = SeedConstants.DefaultLocalitySlug,
                DisplayOrder = 1,
                IsActive = true
            });
        }

        if (!await db.Categories.AnyAsync())
        {
            var order = 1;
            foreach (var name in SeedConstants.Categories)
            {
                db.Categories.Add(new Category
                {
                    Name = name,
                    Slug = SlugHelper.Generate(name),
                    DisplayOrder = order++,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
