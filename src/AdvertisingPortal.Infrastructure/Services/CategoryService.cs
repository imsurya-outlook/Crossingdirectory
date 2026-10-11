using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;

    public CategoryService(ApplicationDbContext db) => _db = db;

    public async Task<List<CategoryDto>> GetActiveAsync() =>
        await Project(_db.Categories.AsNoTracking().Where(c => c.IsActive)).ToListAsync();

    public async Task<List<CategoryDto>> GetAllAsync() =>
        await Project(_db.Categories.AsNoTracking()).ToListAsync();

    public async Task<CategoryDto?> GetByIdAsync(int id) =>
        await Project(_db.Categories.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync();

    public async Task<int?> GetIdBySlugAsync(string slug) =>
        await _db.Categories.Where(c => c.Slug == slug && c.IsActive).Select(c => (int?)c.Id).FirstOrDefaultAsync();

    public async Task<(bool ok, string? error)> CreateAsync(string name, int displayOrder, bool isActive)
    {
        name = name.Trim();
        var slug = SlugHelper.Generate(name);

        if (await _db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower() || c.Slug == slug))
            return (false, "A category with this name already exists.");

        _db.Categories.Add(new Category { Name = name, Slug = slug, DisplayOrder = displayOrder, IsActive = isActive });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error)> UpdateAsync(int id, string name, int displayOrder, bool isActive)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return (false, "Category not found.");

        name = name.Trim();
        var slug = SlugHelper.Generate(name);

        if (await _db.Categories.AnyAsync(c => c.Id != id && (c.Name.ToLower() == name.ToLower() || c.Slug == slug)))
            return (false, "Another category with this name already exists.");

        category.Name = name;
        category.Slug = slug;
        category.DisplayOrder = displayOrder;
        category.IsActive = isActive;
        category.UpdatedOnUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return (false, "Category not found.");

        if (await _db.Advertisements.AnyAsync(a => a.CategoryId == id))
            return (false, "This category is used by advertisements. Deactivate it instead.");

        category.IsDeleted = true;
        category.IsActive = false;
        category.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task ToggleActiveAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return;

        category.IsActive = !category.IsActive;
        category.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private IQueryable<CategoryDto> Project(IQueryable<Category> query) =>
        query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                AdvertisementCount = c.Advertisements.Count
            });
}
