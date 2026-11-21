using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AdvertisingPortal.Infrastructure.Services;

/// <summary>
/// View and click tracking.
///
/// Views are de-duplicated per visitor: the same browser (or the same signed-in
/// account) can only add one view per advertisement inside
/// <see cref="ViewWindow"/>. A refresh, a back button or a re-open in the same
/// tab therefore does not inflate the counter. A different browser, a private
/// window or the window simply expiring does count again, which is the intended
/// "unique-ish visitor" behaviour for a directory listing.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    /// <summary>How long a single visitor is remembered for one advertisement.</summary>
    public static readonly TimeSpan ViewWindow = TimeSpan.FromHours(6);

    private const string CachePrefix = "adview:";

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public AnalyticsService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<bool> TryIncrementViewAsync(int advertisementId, string? viewerKey)
    {
        // No identity to hang the window on: count the request rather than lose the data.
        if (!string.IsNullOrWhiteSpace(viewerKey))
        {
            var cacheKey = CachePrefix + advertisementId + ":" + viewerKey;
            if (_cache.TryGetValue(cacheKey, out _)) return false;

            _cache.Set(cacheKey, true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ViewWindow,
                Size = 1
            });
        }

        var analytics = await GetOrCreateAsync(advertisementId);
        if (analytics is null) return false;

        analytics.ViewCount += 1;
        analytics.LastViewedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
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
