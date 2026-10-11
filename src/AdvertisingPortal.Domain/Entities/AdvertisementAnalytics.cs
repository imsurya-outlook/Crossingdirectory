using AdvertisingPortal.Domain.Common;

namespace AdvertisingPortal.Domain.Entities;

public class AdvertisementAnalytics : BaseEntity
{
    public int AdvertisementId { get; set; }
    public Advertisement Advertisement { get; set; } = default!;

    public int ViewCount { get; set; }
    public int DetailsClickCount { get; set; }

    public DateTime? LastViewedOnUtc { get; set; }
    public DateTime? LastDetailsClickedOnUtc { get; set; }
}
