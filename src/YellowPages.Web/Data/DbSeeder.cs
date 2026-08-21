using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YellowPages.Web.Models;
using YellowPages.Web.Services;

namespace YellowPages.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration config, ILogger logger)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Seed:AdminPassword is not configured, so NO administrator account was created. " +
                "Set it in appsettings.Development.json, user-secrets, or App Service settings and restart.");
        }
        else if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumber = config["Seed:AdminMobile"] ?? "+910000000000",
                    PhoneNumberConfirmed = true,
                    FullName = "Directory Administrator"
                };
                var created = await userManager.CreateAsync(admin, adminPassword);
                if (created.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                    logger.LogInformation("Seeded administrator account {Email}", adminEmail);
                }
                else
                {
                    // Most often the password fails the policy (10+ chars, upper, lower,
                    // digit, symbol) — without this log the failure is invisible.
                    logger.LogError("Could not seed administrator {Email}: {Errors}",
                        adminEmail, string.Join("; ", created.Errors.Select(x => x.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                    logger.LogInformation("Added {Email} to the {Role} role", adminEmail, AppRoles.Admin);
                }

                // Recovery path for a forgotten seeded password: set Seed:ResetAdminPassword
                // for one run rather than deleting the database.
                if (config.GetValue<bool>("Seed:ResetAdminPassword"))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                    var reset = await userManager.ResetPasswordAsync(admin, token, adminPassword);
                    if (reset.Succeeded)
                        logger.LogWarning("Administrator password for {Email} was reset from configuration. " +
                                          "Set Seed:ResetAdminPassword back to false.", adminEmail);
                    else
                        logger.LogError("Could not reset administrator password: {Errors}",
                            string.Join("; ", reset.Errors.Select(x => x.Description)));
                }
            }
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(DefaultCategories());
            await db.SaveChangesAsync();
        }

        if (config.GetValue<bool>("Seed:SeedSampleAds") && !await db.Advertisements.AnyAsync())
        {
            var owner = await userManager.FindByEmailAsync(adminEmail ?? "");
            if (owner is not null)
                await SeedSampleAdsAsync(db, owner, config["Directory:CityName"] ?? "Noida");
        }
    }

    private static IEnumerable<Category> DefaultCategories() => new List<Category>
    {
        new() { Name = "Plumbers",              Slug = "plumbers",              Icon = "bi-droplet-half",    DisplayOrder = 1 },
        new() { Name = "Electricians",          Slug = "electricians",          Icon = "bi-lightning-charge", DisplayOrder = 2 },
        new() { Name = "Carpenters",            Slug = "carpenters",            Icon = "bi-hammer",          DisplayOrder = 3 },
        new() { Name = "AC & Appliance Repair", Slug = "ac-appliance-repair",   Icon = "bi-fan",             DisplayOrder = 4 },
        new() { Name = "Home Cleaning",         Slug = "home-cleaning",         Icon = "bi-bucket",          DisplayOrder = 5 },
        new() { Name = "Packers & Movers",      Slug = "packers-movers",        Icon = "bi-truck",           DisplayOrder = 6 },
        new() { Name = "Tutors & Coaching",     Slug = "tutors-coaching",       Icon = "bi-mortarboard",     DisplayOrder = 7 },
        new() { Name = "Doctors & Clinics",     Slug = "doctors-clinics",       Icon = "bi-heart-pulse",     DisplayOrder = 8 },
        new() { Name = "Salons & Spa",          Slug = "salons-spa",            Icon = "bi-scissors",        DisplayOrder = 9 },
        new() { Name = "Restaurants & Caterers",Slug = "restaurants-caterers",  Icon = "bi-cup-hot",         DisplayOrder = 10 },
        new() { Name = "Car & Bike Service",    Slug = "car-bike-service",      Icon = "bi-car-front",       DisplayOrder = 11 },
        new() { Name = "Real Estate Agents",    Slug = "real-estate-agents",    Icon = "bi-house-door",      DisplayOrder = 12 },
        new() { Name = "Pest Control",          Slug = "pest-control",          Icon = "bi-bug",             DisplayOrder = 13 },
        new() { Name = "Event & Photography",   Slug = "event-photography",     Icon = "bi-camera",          DisplayOrder = 14 }
    };

    private static async Task SeedSampleAdsAsync(AppDbContext db, ApplicationUser owner, string city)
    {
        var plumbers = await db.Categories.FirstAsync(c => c.Slug == "plumbers");
        var electricians = await db.Categories.FirstAsync(c => c.Slug == "electricians");
        var cleaning = await db.Categories.FirstAsync(c => c.Slug == "home-cleaning");

        var ads = new List<Advertisement>
        {
            new Advertisement
            {
                BusinessName = "Sharma Plumbing Works",
                Slug = SlugBuilder.Create("Sharma Plumbing Works"),
                Description = "Family-run plumbing service operating in the sector for 18 years. Same-day call-outs, licensed technicians, and a 30-day workmanship guarantee on every job.",
                CategoryId = plumbers.Id,
                OwnerId = owner.Id,
                AddressLine = "Shop 14, Sector 62 Market",
                Locality = "Sector 62",
                City = city,
                ContactPhone = "+919810000001",
                OpeningHours = "Mon-Sat 8:00 - 20:00",
                Status = AdStatus.Approved,
                IsFeatured = true,
                ReviewedUtc = DateTime.UtcNow,
                ServiceOfferings = new List<ServiceOffering>
                {
                    new() { Name = "Leak detection & repair", PriceFrom = 350, PriceUnit = "per visit", SortOrder = 1 },
                    new() { Name = "Bathroom fitting installation", PriceFrom = 1200, PriceUnit = "per fitting", SortOrder = 2 },
                    new() { Name = "Water tank cleaning", PriceFrom = 900, PriceUnit = "per tank", SortOrder = 3 },
                    new() { Name = "Emergency blockage clearing", PriceFrom = 600, PriceUnit = "per visit", SortOrder = 4 }
                }
            }
        };

        // Additional sample listings
        ads.Add(new Advertisement
        {
            BusinessName = "Bright Spark Electricals",
            Slug = SlugBuilder.Create("Bright Spark Electricals"),
            Description = "Certified electricians for homes, shops, and small offices. Panel upgrades, inverter installation, and preventive wiring audits with written safety reports.",
            CategoryId = electricians.Id,
            OwnerId = owner.Id,
            AddressLine = "B-22, Ground Floor, Sector 18",
            Locality = "Sector 18",
            City = city,
            ContactPhone = "+919810000002",
            OpeningHours = "Mon-Sun 9:00 - 21:00",
            Status = AdStatus.Approved,
            ReviewedUtc = DateTime.UtcNow,
            ServiceOfferings = new List<ServiceOffering>
            {
                new() { Name = "House wiring & rewiring", PriceFrom = 15000, PriceUnit = "per 2BHK", SortOrder = 1 },
                new() { Name = "Inverter & battery installation", PriceFrom = 1500, PriceUnit = "per unit", SortOrder = 2 },
                new() { Name = "Electrical safety audit", PriceFrom = 800, PriceUnit = "per premises", SortOrder = 3 }
            }
        });
        ads.Add(new Advertisement
        {
            BusinessName = "FreshNest Home Cleaning",
            Slug = SlugBuilder.Create("FreshNest Home Cleaning"),
            Description = "Deep-cleaning crews with background-verified staff, eco-friendly chemicals, and fixed-price packages for flats from 1BHK to 4BHK.",
            CategoryId = cleaning.Id,
            OwnerId = owner.Id,
            AddressLine = "Plot 9, Sector 137",
            Locality = "Sector 137",
            City = city,
            ContactPhone = "+919810000003",
            OpeningHours = "Mon-Sat 8:30 - 18:30",
            Status = AdStatus.Pending,
            ServiceOfferings = new List<ServiceOffering>
            {
                new() { Name = "Full home deep clean", PriceFrom = 3500, PriceUnit = "per 2BHK", SortOrder = 1 },
                new() { Name = "Sofa & carpet shampooing", PriceFrom = 700, PriceUnit = "per seat", SortOrder = 2 },
                new() { Name = "Kitchen degreasing", PriceFrom = 1400, PriceUnit = "per kitchen", SortOrder = 3 }
            }
        });

        db.Advertisements.AddRange(ads);
        await db.SaveChangesAsync();
    }

    // Placeholder helper kept private so the sample list above stays readable.
    private static Advertisement BysNamePlaceholder() => new();
}
