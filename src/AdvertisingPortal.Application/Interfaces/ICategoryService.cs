using AdvertisingPortal.Application.DTOs;

namespace AdvertisingPortal.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetActiveAsync();
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<int?> GetIdBySlugAsync(string slug);
    Task<(bool ok, string? error)> CreateAsync(string name, int displayOrder, bool isActive);
    Task<(bool ok, string? error)> UpdateAsync(int id, string name, int displayOrder, bool isActive);
    Task<(bool ok, string? error)> DeleteAsync(int id);
    Task ToggleActiveAsync(int id);
}
