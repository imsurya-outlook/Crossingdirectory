using AdvertisingPortal.Domain.Common;

namespace AdvertisingPortal.Domain.Entities;

public class AdvertisementRating : BaseEntity
{
    public int AdvertisementId { get; set; }
    public Advertisement Advertisement { get; set; } = default!;

    public string CreatedByUserId { get; set; } = default!;
    public ApplicationUser CreatedByUser { get; set; } = default!;

    /// <summary>Service rating from 1 to 5.</summary>
    public int Score { get; set; }

    public string? Comment { get; set; }
}
