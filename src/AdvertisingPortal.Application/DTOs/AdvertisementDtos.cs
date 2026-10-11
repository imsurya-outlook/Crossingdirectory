using AdvertisingPortal.Domain.Enums;

namespace AdvertisingPortal.Application.DTOs;

public class AdvertisementCreateDto
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string ContactName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string AddressLine { get; set; } = default!;
    public int CategoryId { get; set; }
    public int LocalityId { get; set; }
}

public class AdvertisementUpdateDto : AdvertisementCreateDto
{
    public int Id { get; set; }
}

public class AdvertisementImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}

public class AdvertisementListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string CategorySlug { get; set; } = default!;
    public string LocalityName { get; set; } = default!;
    public string? PrimaryImageUrl { get; set; }
    public string ContactName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public bool IsFeatured { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public string ShortDescription { get; set; } = default!;
}

public class AdvertisementDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string LocalityName { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string ContactName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedOnUtc { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int? MyScore { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public int ViewCount { get; set; }
    public int DetailsClickCount { get; set; }
    public List<AdvertisementImageDto> Images { get; set; } = new();
    public List<AdvertisementRatingDto> Reviews { get; set; } = new();
}

public class AdvertisementRatingDto
{
    public int Id { get; set; }
    public int AdvertisementId { get; set; }
    public string AdvertisementTitle { get; set; } = default!;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public string ReviewerName { get; set; } = default!;
    public DateTime CreatedOnUtc { get; set; }
}

public class AdvertisementSearchDto
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? LocalityId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}

public class AdvertiserAdvertisementDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string LocalityName { get; set; } = default!;
    public AdvertisementStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVerified { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int ImageCount { get; set; }
    public int ViewCount { get; set; }
    public int DetailsClickCount { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class AdminAdvertisementDto : AdvertiserAdvertisementDto
{
    public string OwnerEmail { get; set; } = default!;
    public bool IsActive { get; set; }
    public string? ModerationRemarks { get; set; }
}

public class AdminDashboardDto
{
    public int TotalAdvertisements { get; set; }
    public int PendingAdvertisements { get; set; }
    public int ApprovedAdvertisements { get; set; }
    public int RejectedAdvertisements { get; set; }
    public int FeaturedAdvertisements { get; set; }
    public int VerifiedAdvertisements { get; set; }
    public int TotalCategories { get; set; }
    public int TotalLocalities { get; set; }
    public int TotalAdvertisers { get; set; }
    public int TotalViews { get; set; }
    public int TotalDetailsClicks { get; set; }
    public int TotalRatings { get; set; }
    public double AverageRating { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public int AdvertisementCount { get; set; }
}

public class LocalityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public int AdvertisementCount { get; set; }
}
