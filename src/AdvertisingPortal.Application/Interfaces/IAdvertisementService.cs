using AdvertisingPortal.Application.Common;
using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Domain.Enums;

namespace AdvertisingPortal.Application.Interfaces;

public interface IAdvertisementService
{
    Task<PagedResult<AdvertisementListItemDto>> SearchAsync(AdvertisementSearchDto search);
    Task<List<AdvertisementListItemDto>> GetFeaturedAsync(int take);
    Task<List<AdvertisementListItemDto>> GetRecentAsync(int take);
    Task<AdvertisementDetailsDto?> GetPublicDetailsBySlugAsync(string slug);

    Task<int> CreateAsync(AdvertisementCreateDto dto, IReadOnlyList<UploadedFileDto> images, string userId, bool submitForApproval);
    Task UpdateAsync(AdvertisementUpdateDto dto, IReadOnlyList<UploadedFileDto> newImages, string userId, bool isAdmin, bool submitForApproval);

    Task<Advertisement?> GetOwnedAsync(int id, string userId, bool isAdmin);
    Task<List<AdvertiserAdvertisementDto>> GetAdvertiserAdvertisementsAsync(string userId);
    Task<List<AdminAdvertisementDto>> GetAdminAdvertisementsAsync(AdvertisementStatus? status);

    Task DeleteImageAsync(int imageId, string userId, bool isAdmin);
    Task SetPrimaryImageAsync(int imageId, string userId, bool isAdmin);

    Task SubmitForApprovalAsync(int id, string userId, bool isAdmin);
    Task SetStatusAsync(int id, AdvertisementStatus status, string? remarks);
    Task SetFeaturedAsync(int id, bool featured);
    Task SetVerifiedAsync(int id, bool verified);
    Task SetActiveAsync(int id, bool isActive);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<string?> GetSlugAsync(int id);
}
