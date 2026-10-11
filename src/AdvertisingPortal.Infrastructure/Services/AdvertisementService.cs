using AdvertisingPortal.Application.Common;
using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Domain.Enums;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Services;

public class AdvertisementService : IAdvertisementService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public AdvertisementService(ApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    private IQueryable<Advertisement> PublicQuery() =>
        _db.Advertisements
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Locality)
            .Include(a => a.Images)
            .Where(a => a.Status == AdvertisementStatus.Approved
                        && a.IsActive
                        && a.Category.IsActive
                        && a.Locality.IsActive);

    public async Task<PagedResult<AdvertisementListItemDto>> SearchAsync(AdvertisementSearchDto search)
    {
        var query = PublicQuery();

        if (!string.IsNullOrWhiteSpace(search.Keyword))
        {
            var keyword = search.Keyword.Trim();
            query = query.Where(a => a.Title.Contains(keyword)
                                     || a.Description.Contains(keyword)
                                     || a.ContactName.Contains(keyword)
                                     || a.Category.Name.Contains(keyword));
        }

        if (search.CategoryId.HasValue) query = query.Where(a => a.CategoryId == search.CategoryId.Value);
        if (search.LocalityId.HasValue) query = query.Where(a => a.LocalityId == search.LocalityId.Value);

        var total = await query.CountAsync();
        var page = search.Page < 1 ? 1 : search.Page;
        var pageSize = search.PageSize < 1 ? 9 : search.PageSize;

        var items = await query
            .OrderByDescending(a => a.IsFeatured)
            .ThenByDescending(a => a.IsVerified)
            .ThenByDescending(a => a.RatingCount == 0 ? 0 : (double)a.RatingSum / a.RatingCount)
            .ThenByDescending(a => a.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AdvertisementListItemDto>
        {
            Items = items.Select(MapListItem).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<AdvertisementListItemDto>> GetFeaturedAsync(int take)
    {
        var items = await PublicQuery()
            .Where(a => a.IsFeatured)
            .OrderByDescending(a => a.IsVerified)
            .ThenByDescending(a => a.RatingCount == 0 ? 0 : (double)a.RatingSum / a.RatingCount)
            .ThenByDescending(a => a.CreatedOnUtc)
            .Take(take)
            .ToListAsync();

        return items.Select(MapListItem).ToList();
    }

    // Verified businesses first, then the best rated, then the newest.
    public async Task<List<AdvertisementListItemDto>> GetRecentAsync(int take)
    {
        var items = await PublicQuery()
            .OrderByDescending(a => a.IsVerified)
            .ThenByDescending(a => a.RatingCount == 0 ? 0 : (double)a.RatingSum / a.RatingCount)
            .ThenByDescending(a => a.RatingCount)
            .ThenByDescending(a => a.CreatedOnUtc)
            .Take(take)
            .ToListAsync();

        return items.Select(MapListItem).ToList();
    }

    public async Task<AdvertisementDetailsDto?> GetPublicDetailsBySlugAsync(string slug)
    {
        var ad = await PublicQuery()
            .Include(a => a.Analytics)
            .FirstOrDefaultAsync(a => a.Slug == slug);

        if (ad is null) return null;

        var reviews = await _db.AdvertisementRatings
            .AsNoTracking()
            .Where(r => r.AdvertisementId == ad.Id)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(10)
            .Select(r => new AdvertisementRatingDto
            {
                Id = r.Id,
                AdvertisementId = r.AdvertisementId,
                AdvertisementTitle = ad.Title,
                Score = r.Score,
                Comment = r.Comment,
                ReviewerName = r.CreatedByUser.FullName ?? r.CreatedByUser.Email ?? "Registered user",
                CreatedOnUtc = r.CreatedOnUtc
            })
            .ToListAsync();

        return new AdvertisementDetailsDto
        {
            Id = ad.Id,
            Title = ad.Title,
            Slug = ad.Slug,
            Description = ad.Description,
            CategoryName = ad.Category.Name,
            LocalityName = ad.Locality.Name,
            AddressLine = ad.AddressLine,
            ContactName = ad.ContactName,
            PhoneNumber = ad.PhoneNumber,
            Email = ad.Email,
            WebsiteUrl = ad.WebsiteUrl,
            IsFeatured = ad.IsFeatured,
            IsVerified = ad.IsVerified,
            VerifiedOnUtc = ad.VerifiedOnUtc,
            AverageRating = ad.AverageRating,
            RatingCount = ad.RatingCount,
            CreatedOnUtc = ad.CreatedOnUtc,
            ViewCount = ad.Analytics?.ViewCount ?? 0,
            DetailsClickCount = ad.Analytics?.DetailsClickCount ?? 0,
            Reviews = reviews,
            Images = ad.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => new AdvertisementImageDto
                {
                    Id = i.Id,
                    ImageUrl = _storage.GetPublicUrl(i.FilePath),
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder
                }).ToList()
        };
    }

    public async Task<int> CreateAsync(AdvertisementCreateDto dto, IReadOnlyList<UploadedFileDto> images, string userId, bool submitForApproval)
    {
        var ad = new Advertisement
        {
            Title = dto.Title.Trim(),
            Slug = await BuildUniqueSlugAsync(dto.Title, null),
            Description = dto.Description.Trim(),
            ContactName = dto.ContactName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Email = dto.Email?.Trim(),
            WebsiteUrl = dto.WebsiteUrl?.Trim(),
            AddressLine = dto.AddressLine.Trim(),
            CategoryId = dto.CategoryId,
            LocalityId = dto.LocalityId,
            CreatedByUserId = userId,
            Status = submitForApproval ? AdvertisementStatus.PendingApproval : AdvertisementStatus.Draft,
            IsActive = true,
            Analytics = new AdvertisementAnalytics()
        };

        _db.Advertisements.Add(ad);
        await _db.SaveChangesAsync();

        await AddImagesAsync(ad, images);
        await _db.SaveChangesAsync();

        return ad.Id;
    }

    public async Task UpdateAsync(AdvertisementUpdateDto dto, IReadOnlyList<UploadedFileDto> newImages, string userId, bool isAdmin, bool submitForApproval)
    {
        var ad = await _db.Advertisements
            .Include(a => a.Images)
            .FirstOrDefaultAsync(a => a.Id == dto.Id);

        if (ad is null) throw new InvalidOperationException("Advertisement not found.");
        if (!isAdmin && ad.CreatedByUserId != userId) throw new UnauthorizedAccessException();

        if (!string.Equals(ad.Title, dto.Title.Trim(), StringComparison.OrdinalIgnoreCase))
            ad.Slug = await BuildUniqueSlugAsync(dto.Title, ad.Id);

        ad.Title = dto.Title.Trim();
        ad.Description = dto.Description.Trim();
        ad.ContactName = dto.ContactName.Trim();
        ad.PhoneNumber = dto.PhoneNumber.Trim();
        ad.Email = dto.Email?.Trim();
        ad.WebsiteUrl = dto.WebsiteUrl?.Trim();
        ad.AddressLine = dto.AddressLine.Trim();
        ad.CategoryId = dto.CategoryId;
        ad.LocalityId = dto.LocalityId;
        ad.UpdatedOnUtc = DateTime.UtcNow;

        if (submitForApproval && !isAdmin)
        {
            ad.Status = AdvertisementStatus.PendingApproval;

            // Edited content has to be verified again before the badge is shown.
            ad.IsVerified = false;
            ad.VerifiedOnUtc = null;
        }

        await AddImagesAsync(ad, newImages);
        await _db.SaveChangesAsync();
    }

    private async Task AddImagesAsync(Advertisement ad, IReadOnlyList<UploadedFileDto> images)
    {
        if (images.Count == 0) return;

        var existingCount = await _db.AdvertisementImages.CountAsync(i => i.AdvertisementId == ad.Id);
        var order = existingCount;

        foreach (var file in images)
        {
            if (existingCount >= UploadLimits.MaxImagesPerAdvertisement) break;
            if (file.Length <= 0 || file.Length > UploadLimits.MaxImageSizeBytes) continue;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!UploadLimits.AllowedExtensions.Contains(extension)) continue;
            if (!UploadLimits.AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant())) continue;

            var saved = await _storage.SaveAsync(file, "advertisements/" + ad.Id);

            _db.AdvertisementImages.Add(new AdvertisementImage
            {
                AdvertisementId = ad.Id,
                FileName = saved.FileName,
                FilePath = saved.FilePath,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                DisplayOrder = order++,
                IsPrimary = existingCount == 0 && order == 1
            });

            existingCount++;
        }
    }

    public async Task<Advertisement?> GetOwnedAsync(int id, string userId, bool isAdmin)
    {
        var ad = await _db.Advertisements
            .Include(a => a.Images)
            .Include(a => a.Category)
            .Include(a => a.Locality)
            .Include(a => a.Analytics)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (ad is null) return null;
        if (!isAdmin && ad.CreatedByUserId != userId) return null;
        return ad;
    }

    public async Task<List<AdvertiserAdvertisementDto>> GetAdvertiserAdvertisementsAsync(string userId)
    {
        var ads = await _db.Advertisements
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Locality)
            .Include(a => a.Images)
            .Include(a => a.Analytics)
            .Where(a => a.CreatedByUserId == userId)
            .OrderByDescending(a => a.CreatedOnUtc)
            .ToListAsync();

        return ads.Select(a => new AdvertiserAdvertisementDto
        {
            Id = a.Id,
            Title = a.Title,
            Slug = a.Slug,
            CategoryName = a.Category.Name,
            LocalityName = a.Locality.Name,
            Status = a.Status,
            IsFeatured = a.IsFeatured,
            IsVerified = a.IsVerified,
            AverageRating = a.AverageRating,
            RatingCount = a.RatingCount,
            ImageCount = a.Images.Count,
            ViewCount = a.Analytics?.ViewCount ?? 0,
            DetailsClickCount = a.Analytics?.DetailsClickCount ?? 0,
            CreatedOnUtc = a.CreatedOnUtc,
            PrimaryImageUrl = PrimaryUrl(a)
        }).ToList();
    }

    public async Task<List<AdminAdvertisementDto>> GetAdminAdvertisementsAsync(AdvertisementStatus? status)
    {
        var query = _db.Advertisements
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Locality)
            .Include(a => a.Images)
            .Include(a => a.Analytics)
            .Include(a => a.CreatedByUser)
            .AsQueryable();

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        var ads = await query.OrderByDescending(a => a.CreatedOnUtc).ToListAsync();

        return ads.Select(a => new AdminAdvertisementDto
        {
            Id = a.Id,
            Title = a.Title,
            Slug = a.Slug,
            CategoryName = a.Category.Name,
            LocalityName = a.Locality.Name,
            Status = a.Status,
            IsFeatured = a.IsFeatured,
            IsVerified = a.IsVerified,
            AverageRating = a.AverageRating,
            RatingCount = a.RatingCount,
            IsActive = a.IsActive,
            ImageCount = a.Images.Count,
            ViewCount = a.Analytics?.ViewCount ?? 0,
            DetailsClickCount = a.Analytics?.DetailsClickCount ?? 0,
            CreatedOnUtc = a.CreatedOnUtc,
            OwnerEmail = a.CreatedByUser.Email ?? "unknown",
            ModerationRemarks = a.ModerationRemarks,
            PrimaryImageUrl = PrimaryUrl(a)
        }).ToList();
    }

    public async Task DeleteImageAsync(int imageId, string userId, bool isAdmin)
    {
        var image = await _db.AdvertisementImages
            .Include(i => i.Advertisement)
            .FirstOrDefaultAsync(i => i.Id == imageId);

        if (image is null) return;
        if (!isAdmin && image.Advertisement.CreatedByUserId != userId) throw new UnauthorizedAccessException();

        await _storage.DeleteAsync(image.FilePath);
        _db.AdvertisementImages.Remove(image);
        await _db.SaveChangesAsync();

        if (image.IsPrimary)
        {
            var next = await _db.AdvertisementImages
                .Where(i => i.AdvertisementId == image.AdvertisementId)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefaultAsync();

            if (next is not null)
            {
                next.IsPrimary = true;
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task SetPrimaryImageAsync(int imageId, string userId, bool isAdmin)
    {
        var image = await _db.AdvertisementImages
            .Include(i => i.Advertisement)
            .FirstOrDefaultAsync(i => i.Id == imageId);

        if (image is null) return;
        if (!isAdmin && image.Advertisement.CreatedByUserId != userId) throw new UnauthorizedAccessException();

        var siblings = await _db.AdvertisementImages
            .Where(i => i.AdvertisementId == image.AdvertisementId)
            .ToListAsync();

        foreach (var sibling in siblings)
            sibling.IsPrimary = sibling.Id == imageId;

        await _db.SaveChangesAsync();
    }

    public async Task SubmitForApprovalAsync(int id, string userId, bool isAdmin)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
        if (ad is null) return;
        if (!isAdmin && ad.CreatedByUserId != userId) throw new UnauthorizedAccessException();

        ad.Status = AdvertisementStatus.PendingApproval;
        ad.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(int id, AdvertisementStatus status, string? remarks)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
        if (ad is null) return;

        ad.Status = status;
        ad.ModerationRemarks = remarks;
        ad.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetFeaturedAsync(int id, bool featured)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
        if (ad is null) return;

        ad.IsFeatured = featured;
        ad.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetVerifiedAsync(int id, bool verified)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
        if (ad is null) return;

        ad.IsVerified = verified;
        ad.VerifiedOnUtc = verified ? DateTime.UtcNow : null;
        ad.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
        if (ad is null) return;

        ad.IsActive = isActive;
        ad.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var ratingCount = await _db.AdvertisementRatings.CountAsync();
        var ratingSum = await _db.AdvertisementRatings.SumAsync(r => (int?)r.Score) ?? 0;

        return new AdminDashboardDto
        {
            TotalAdvertisements = await _db.Advertisements.CountAsync(),
            PendingAdvertisements = await _db.Advertisements.CountAsync(a => a.Status == AdvertisementStatus.PendingApproval),
            ApprovedAdvertisements = await _db.Advertisements.CountAsync(a => a.Status == AdvertisementStatus.Approved),
            RejectedAdvertisements = await _db.Advertisements.CountAsync(a => a.Status == AdvertisementStatus.Rejected),
            FeaturedAdvertisements = await _db.Advertisements.CountAsync(a => a.IsFeatured),
            VerifiedAdvertisements = await _db.Advertisements.CountAsync(a => a.IsVerified),
            TotalCategories = await _db.Categories.CountAsync(),
            TotalLocalities = await _db.Localities.CountAsync(),
            TotalAdvertisers = await _db.Advertisements.Select(a => a.CreatedByUserId).Distinct().CountAsync(),
            TotalViews = await _db.AdvertisementAnalytics.SumAsync(a => (int?)a.ViewCount) ?? 0,
            TotalDetailsClicks = await _db.AdvertisementAnalytics.SumAsync(a => (int?)a.DetailsClickCount) ?? 0,
            TotalRatings = ratingCount,
            AverageRating = ratingCount == 0 ? 0 : Math.Round((double)ratingSum / ratingCount, 1)
        };
    }

    public async Task<string?> GetSlugAsync(int id)
        => await _db.Advertisements.Where(a => a.Id == id).Select(a => a.Slug).FirstOrDefaultAsync();

    private string? PrimaryUrl(Advertisement ad)
    {
        var image = ad.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).FirstOrDefault();
        return image is null ? null : _storage.GetPublicUrl(image.FilePath);
    }

    private AdvertisementListItemDto MapListItem(Advertisement ad) => new()
    {
        Id = ad.Id,
        Title = ad.Title,
        Slug = ad.Slug,
        CategoryName = ad.Category.Name,
        CategorySlug = ad.Category.Slug,
        LocalityName = ad.Locality.Name,
        ContactName = ad.ContactName,
        PhoneNumber = ad.PhoneNumber,
        IsFeatured = ad.IsFeatured,
        IsVerified = ad.IsVerified,
        AverageRating = ad.AverageRating,
        RatingCount = ad.RatingCount,
        PrimaryImageUrl = PrimaryUrl(ad),
        ShortDescription = ad.Description.Length > 140 ? ad.Description[..140] + "..." : ad.Description
    };

    private async Task<string> BuildUniqueSlugAsync(string title, int? currentId)
    {
        var baseSlug = SlugHelper.Generate(title);
        var slug = baseSlug;
        var suffix = 1;

        while (await _db.Advertisements.AnyAsync(a => a.Slug == slug && (currentId == null || a.Id != currentId)))
            slug = baseSlug + "-" + (++suffix);

        return slug;
    }
}
