using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Infrastructure.Data;
using AdvertisingPortal.Infrastructure.Services;
using AdvertisingPortal.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdvertisingPortal.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the DbContext against the provider named in Database:Provider.
    /// Supported values: Sqlite (DEV), Postgres (PROD).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? DbProviders.Sqlite;
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=advertisingportal.db";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (provider.Equals(DbProviders.Postgres, StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
            else
                options.UseSqlite(connectionString);
        });

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var storageProvider = configuration["Storage:Provider"] ?? "Local";
        if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        else
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<IAdvertisementService, AdvertisementService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ILocalityService, LocalityService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IShopImageService, ShopImageService>();

        return services;
    }

    /// <summary>True when the configured provider is PostgreSQL.</summary>
    public static bool IsPostgres(IConfiguration configuration) =>
        (configuration["Database:Provider"] ?? DbProviders.Sqlite)
            .Equals(DbProviders.Postgres, StringComparison.OrdinalIgnoreCase);
}
