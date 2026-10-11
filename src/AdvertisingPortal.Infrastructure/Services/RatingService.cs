using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Domain.Enums;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Services;

public class RatingService : IRatingService
{
    private readonly ApplicationDbContext _db;

    public RatingService(ApplicationDbContext db) => _db = db;

    public async Task<(bool ok, string? error)> SubmitAsync(int advertisementId, string userId, int score, string? comment)
    {
        if (score < 1 || score > 5) return (false, "Please choose a rating between 1 and 5 stars.");

        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == advertisementId);
        if (ad is null) return (false, "Advertisement not found.");
        if (ad.Status != AdvertisementStatus.Approved || !ad.IsActive) return (false, "This advertisement is not open for ratings.");
        if (ad.CreatedByUserId == userId) return (false, "You cannot rate your own advertisement.");

        var existing = await _db.AdvertisementRatings
            .FirstOrDefaultAsync(r => r.AdvertisementId == advertisementId && r.CreatedByUserId == userId);

        if (existing is null)
        {
            _db.AdvertisementRatings.Add(new AdvertisementRating
            {
                AdvertisementId = advertisementId,
                CreatedByUserId = userId,
                Score = score,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            });
        }
        else
        {
            existing.Score = score;
            existing.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            existing.UpdatedOnUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await RecalculateAsync(advertisementId);

        return (true, null);
    }

    public async Task<int?> GetUserScoreAsync(int advertisementId, string userId)
        => await _db.AdvertisementRatings
            .Where(r => r.AdvertisementId == advertisementId && r.CreatedByUserId == userId)
            .Select(r => (int?)r.Score)
            .FirstOrDefaultAsync();

    public async Task<List<AdvertisementRatingDto>> GetForAdvertisementAsync(int advertisementId, int take)
        => await Project(_db.AdvertisementRatings.AsNoTracking().Where(r => r.AdvertisementId == advertisementId))
            .Take(take)
            .ToListAsync();

    public async Task<List<AdvertisementRatingDto>> GetAllAsync(int? advertisementId)
    {
        var query = _db.AdvertisementRatings.AsNoTracking();
        if (advertisementId.HasValue) query = query.Where(r => r.AdvertisementId == advertisementId.Value);

        return await Project(query).ToListAsync();
    }

    public async Task DeleteAsync(int ratingId)
    {
        var rating = await _db.AdvertisementRatings.FirstOrDefaultAsync(r => r.Id == ratingId);
        if (rating is null) return;

        var advertisementId = rating.AdvertisementId;
        _db.AdvertisementRatings.Remove(rating);
        await _db.SaveChangesAsync();
        await RecalculateAsync(advertisementId);
    }

    private async Task RecalculateAsync(int advertisementId)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == advertisementId);
        if (ad is null) return;

        var scores = await _db.AdvertisementRatings
            .Where(r => r.AdvertisementId == advertisementId)
            .Select(r => r.Score)
            .ToListAsync();

        ad.RatingCount = scores.Count;
        ad.RatingSum = scores.Sum();
        ad.UpdatedOnUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private static IQueryable<AdvertisementRatingDto> Project(IQueryable<AdvertisementRating> query)
        => query.OrderByDescending(r => r.CreatedOnUtc)
            .Select(r => new AdvertisementRatingDto
            {
                Id = r.Id,
                AdvertisementId = r.AdvertisementId,
                AdvertisementTitle = r.Advertisement.Title,
                Score = r.Score,
                Comment = r.Comment,
                ReviewerName = r.CreatedByUser.FullName ?? r.CreatedByUser.Email ?? "Registered user",
                CreatedOnUtc = r.CreatedOnUtc
            });
}
