
namespace AdvertisingPortal.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Records one view of an advertisement, but only when this visitor has not
    /// already been counted for this ad inside the de-duplication window.
    /// Returns true when the view was actually counted.
    /// </summary>
    Task<bool> TryIncrementViewAsync(int advertisementId, string? viewerKey);

    Task<string?> RegisterDetailsClickAndGetSlugAsync(int advertisementId);
}
