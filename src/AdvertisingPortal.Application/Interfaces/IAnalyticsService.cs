namespace AdvertisingPortal.Application.Interfaces;

public interface IAnalyticsService
{
    Task IncrementViewAsync(int advertisementId);
    Task<string?> RegisterDetailsClickAndGetSlugAsync(int advertisementId);
}
