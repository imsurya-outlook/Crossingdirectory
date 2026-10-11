using AdvertisingPortal.Application.DTOs;

namespace AdvertisingPortal.Application.Interfaces;

public interface IRatingService
{
    Task<(bool ok, string? error)> SubmitAsync(int advertisementId, string userId, int score, string? comment);
    Task<int?> GetUserScoreAsync(int advertisementId, string userId);
    Task<List<AdvertisementRatingDto>> GetForAdvertisementAsync(int advertisementId, int take);
    Task<List<AdvertisementRatingDto>> GetAllAsync(int? advertisementId);
    Task DeleteAsync(int ratingId);
}
