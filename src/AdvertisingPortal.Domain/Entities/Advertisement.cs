using System.ComponentModel.DataAnnotations.Schema;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Enums;

namespace AdvertisingPortal.Domain.Entities;

public class Advertisement : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Description { get; set; } = default!;

    public string ContactName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string AddressLine { get; set; } = default!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public int LocalityId { get; set; }
    public Locality Locality { get; set; } = default!;

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public AdvertisementStatus Status { get; set; } = AdvertisementStatus.Draft;
    public string? ModerationRemarks { get; set; }

    // Admin marks this true only after verifying the business personally.
    public bool IsVerified { get; set; }
    public DateTime? VerifiedOnUtc { get; set; }

    // Denormalised rating aggregates, recalculated whenever a rating is written.
    public int RatingCount { get; set; }
    public int RatingSum { get; set; }

    [NotMapped]
    public double AverageRating => RatingCount == 0 ? 0 : Math.Round((double)RatingSum / RatingCount, 1);

    public string CreatedByUserId { get; set; } = default!;
    public ApplicationUser CreatedByUser { get; set; } = default!;

    public ICollection<AdvertisementImage> Images { get; set; } = new List<AdvertisementImage>();
    public ICollection<AdvertisementRating> Ratings { get; set; } = new List<AdvertisementRating>();
    public AdvertisementAnalytics? Analytics { get; set; }
}
