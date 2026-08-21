using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YellowPages.Web.Data;
using YellowPages.Web.Infrastructure;
using YellowPages.Web.Models;
using YellowPages.Web.Options;
using YellowPages.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<DirectoryOptions>(builder.Configuration.GetSection("Directory"));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes of entropy.");

// ---------------------------------------------------------------- data access
// One model, two providers. Nothing above the DbContext is provider-specific, so
// moving from the SQLite pilot to Azure SQL is a configuration change plus the
// matching migration set — not a code rewrite.
var dbProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var useSqlServer = dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
var commandTimeout = builder.Configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 60;
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=App_Data/yellowpages.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlServer)
    {
        options.UseSqlServer(connectionString, sql =>
        {
            // Azure SQL throttles, patches, and fails over as normal platform behaviour.
            // Without a retry strategy those events surface to users as 500s.
            sql.EnableRetryOnFailure(
                maxRetryCount: 6,
                maxRetryDelay: TimeSpan.FromSeconds(15),
                errorNumbersToAdd: null);
            sql.CommandTimeout(commandTimeout);
        });
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// ---------------------------------------------------------------- identity
// IdentityCore is used deliberately: AddIdentity would register cookie authentication
// as the default scheme and compete with JWT for the same requests.
builder.Services
    .AddIdentityCore<ApplicationUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 10;
        o.Password.RequireDigit = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireNonAlphanumeric = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.SignIn.RequireConfirmedEmail = false;   // enable once an email sender is wired up
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ---------------------------------------------------------------- authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;            // keep "sub" / "role" claim names as issued
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            // Browsers carry the token in an HttpOnly cookie; API clients use the header.
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthCookies.AccessToken, out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            },

            // MVC pages need a redirect to the sign-in screen, not a bare 401.
            OnChallenge = context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    context.HandleResponse();
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
                }
                return Task.CompletedTask;
            },

            OnForbidden = context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                    context.Response.Redirect("/denied");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
});

// ---------------------------------------------------------------- app services
builder.Services.AddScoped<ITokenService, JwtTokenService>();

var storageProvider = builder.Configuration["Storage:Provider"] ?? "LocalDisk";
if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IPhotoStorage, AzureBlobPhotoStorage>();
else
    builder.Services.AddSingleton<IPhotoStorage, LocalDiskPhotoStorage>();

builder.Services.AddControllersWithViews();
builder.Services.AddResponseCompression();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddAntiforgery(o => o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

// Once Azure SQL removes the single-instance constraint, App Service can scale out —
// at which point the Data Protection key ring must be shared. Left on the local
// filesystem, each instance generates its own keys and antiforgery tokens issued by
// one instance fail validation on another.
var dataProtectionConnection = builder.Configuration["DataProtection:BlobConnectionString"];
if (!string.IsNullOrWhiteSpace(dataProtectionConnection))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("yellowpages-local")
        .PersistKeysToAzureBlobStorage(
            dataProtectionConnection,
            builder.Configuration["DataProtection:Container"] ?? "dataprotection-keys",
            "keyring.xml");
}

// Azure App Service terminates TLS at the front end; honour the forwarded scheme so
// Secure cookies and redirect URLs are generated correctly.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// ---------------------------------------------------------------- pipeline
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.MapHealthChecks("/healthz");

// ---------------------------------------------------------------- database bootstrap
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Only the SQLite file provider needs a directory to exist first.
    if (!useSqlServer) Directory.CreateDirectory("App_Data");

    // Migrate() only creates tables if migrations actually exist in the assembly.
    // A fresh clone has none, so an unguarded Migrate() leaves an empty database and
    // the first registration fails with "no such table: AspNetUsers".
    var hasMigrations = db.Database.GetMigrations().Any();
    var useEnsureCreated = app.Configuration.GetValue<bool>("Database:UseEnsureCreated");

    if (useEnsureCreated || !hasMigrations)
    {
        if (!useEnsureCreated)
            logger.LogWarning("No EF migrations found in this assembly; creating the schema with EnsureCreated. " +
                              "Run 'dotnet ef migrations add InitialCreate' before deploying so schema changes are versioned.");
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        // Migrate() carries its own execution strategy, so transient Azure SQL faults
        // during a cold start (or right after a deployment) are retried rather than
        // crashing the container.
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(app.Services, app.Configuration, logger);
}

app.Run();
