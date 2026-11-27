using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Infrastructure;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);
// keeps the window cache from growing without limit on a busy site.
builder.Services.AddMemoryCache(options => options.SizeLimit = 100_000);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ---- Google sign-in ----
// Credentials come from configuration so no secret lives in source:
//   Authentication:Google:ClientId
//   Authentication:Google:ClientSecret
// When either is blank the provider is not registered and the button stays hidden.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services
        .AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;
            options.SignInScheme = IdentityConstants.ExternalScheme;
        });
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// "📞 Call shop" buttons in the WhatsApp bot's search replies link to https://<this site>/c/<code>. The WhatsApp bot app
// owns the code (which user, which shop): forward the tap there; it records it and opens the phone's dialer.
var callLinkTarget = builder.Configuration["CallLinks:TargetBaseUrl"]?.Trim().TrimEnd('/');
app.MapGet("/c/{code}", (string code) =>
    string.IsNullOrEmpty(callLinkTarget)
        || code.Length != 10
        || !code.All(char.IsAsciiLetterOrDigit)
            ? Results.NotFound()
            : Results.Redirect($"{callLinkTarget}/c/{code}"));

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=RouteByShopId}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();

    if (DependencyInjection.IsPostgres(app.Configuration))
    {
        // Production schema is applied from database/postgres/*.sql, so only verify connectivity.
        await db.Database.CanConnectAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    await DbSeeder.SeedAsync(
        db,
        services.GetRequiredService<UserManager<ApplicationUser>>(),
        services.GetRequiredService<RoleManager<IdentityRole>>(),
        app.Configuration);

    var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
    Directory.CreateDirectory(uploadRoot);
}

app.Run();
