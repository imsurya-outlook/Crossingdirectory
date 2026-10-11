using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Services;

public class LocalityService : ILocalityService
{
    private readonly ApplicationDbContext _db;

    public LocalityService(ApplicationDbContext db) => _db = db;

    public async Task<List<LocalityDto>> GetActiveAsync() =>
        await Project(_db.Localities.AsNoTracking().Where(l => l.IsActive)).ToListAsync();

    public async Task<List<LocalityDto>> GetAllAsync() =>
        await Project(_db.Localities.AsNoTracking()).ToListAsync();

    public async Task<LocalityDto?> GetByIdAsync(int id) =>
        await Project(_db.Localities.AsNoTracking().Where(l => l.Id == id)).FirstOrDefaultAsync();

    public async Task<int?> GetIdBySlugAsync(string slug) =>
        await _db.Localities.Where(l => l.Slug == slug && l.IsActive).Select(l => (int?)l.Id).FirstOrDefaultAsync();

    public async Task<int?> GetDefaultLocalityIdAsync()
    {
        var id = await _db.Localities
            .Where(l => l.Slug == SeedConstants.DefaultLocalitySlug && l.IsActive)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (id.HasValue) return id;

        return await _db.Localities
            .Where(l => l.IsActive)
            .OrderBy(l => l.DisplayOrder)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool ok, string? error)> CreateAsync(string name, int displayOrder, bool isActive)
    {
        name = name.Trim();
        var slug = SlugHelper.Generate(name);

        if (await _db.Localities.AnyAsync(l => l.Name.ToLower() == name.ToLower() || l.Slug == slug))
            return (false, "A locality with this name already exists.");

        _db.Localities.Add(new Locality { Name = name, Slug = slug, DisplayOrder = displayOrder, IsActive = isActive });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error)> UpdateAsync(int id, string name, int displayOrder, bool isActive)
    {
        var locality = await _db.Localities.FirstOrDefaultAsync(l => l.Id == id);
        if (locality is null) return (false, "Locality not found.");

        name = name.Trim();
        var slug = SlugHelper.Generate(name);

        if (await _db.Localities.AnyAsync(l => l.Id != id && (l.Name.ToLower() == name.ToLower() || l.Slug == slug)))
            return (false, "Another locality with this name already exists.");

        locality.Name = name;
        locality.Slug = slug;
        locality.DisplayOrder = displayOrder;
        locality.IsActive = isActive;
        locality.UpdatedOnUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id)
    {
        var locality = await _db.Localities.FirstOrDefaultAsync(l => l.Id == id);
        if (locality is null) return (false, "Locality not found.");

        if (await _db.Advertisements.AnyAsync(a => a.LocalityId == id))
            return (false, "This locality is used by advertisements. Deactivate it instead.");

        locality.IsDeleted = true;
        locality.IsActive = false;
        locality.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task ToggleActiveAsync(int id)
    {
        var locality = await _db.Localities.FirstOrDefaultAsync(l => l.Id == id);
        if (locality is null) return;

        locality.IsActive = !locality.IsActive;
        locality.UpdatedOnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private IQueryable<LocalityDto> Project(IQueryable<Locality> query) =>
        query.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Name)
            .Select(l => new LocalityDto
            {
                Id = l.Id,
                Name = l.Name,
                Slug = l.Slug,
                IsActive = l.IsActive,
                DisplayOrder = l.DisplayOrder,
                AdvertisementCount = l.Advertisements.Count
            });
}
