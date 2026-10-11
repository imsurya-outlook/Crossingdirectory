using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public AnalyticsService(ApplicationDbContext db) => _db = db;

    public async Task IncrementViewAsync(int advertisementId)
    {
        var analytics = await GetOrCreateAsync(advertisementId);
        if (analytics is null) return;

        analytics.ViewCount += 1;
        analytics.LastViewedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<string?> RegisterDetailsClickAndGetSlugAsync(int advertisementId)
    {
        var ad = await _db.Advertisements
            .Include(a => a.Analytics)
            .FirstOrDefaultAsync(a => a.Id == advertisementId);

        if (ad is null) return null;

        ad.Analytics ??= new AdvertisementAnalytics { AdvertisementId = ad.Id };
        ad.Analytics.DetailsClickCount += 1;
        ad.Analytics.LastDetailsClickedOnUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ad.Slug;
    }

    private async Task<AdvertisementAnalytics?> GetOrCreateAsync(int advertisementId)
    {
        var ad = await _db.Advertisements
            .Include(a => a.Analytics)
            .FirstOrDefaultAsync(a => a.Id == advertisementId);

        if (ad is null) return null;

        if (ad.Analytics is null)
        {
            ad.Analytics = new AdvertisementAnalytics { AdvertisementId = ad.Id };
            _db.AdvertisementAnalytics.Add(ad.Analytics);
        }

        return ad.Analytics;
    }
}
