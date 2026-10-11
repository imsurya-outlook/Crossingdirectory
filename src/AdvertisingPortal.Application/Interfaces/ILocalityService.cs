using AdvertisingPortal.Application.DTOs;

namespace AdvertisingPortal.Application.Interfaces;

public interface ILocalityService
{
    Task<List<LocalityDto>> GetActiveAsync();
    Task<List<LocalityDto>> GetAllAsync();
    Task<LocalityDto?> GetByIdAsync(int id);
    Task<int?> GetIdBySlugAsync(string slug);
    Task<int?> GetDefaultLocalityIdAsync();
    Task<(bool ok, string? error)> CreateAsync(string name, int displayOrder, bool isActive);
    Task<(bool ok, string? error)> UpdateAsync(int id, string name, int displayOrder, bool isActive);
    Task<(bool ok, string? error)> DeleteAsync(int id);
    Task ToggleActiveAsync(int id);
}
