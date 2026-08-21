using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YellowPages.Web.Data;
using YellowPages.Web.Models;
using YellowPages.Web.Options;
using YellowPages.Web.ViewModels;

namespace YellowPages.Web.Controllers;

/// <summary>Public, anonymous browse + search experience.</summary>
[Route("directory")]
public class DirectoryController : Controller
{
    private readonly AppDbContext _db;
    private readonly DirectoryOptions _options;

    public DirectoryController(AppDbContext db, IOptions<DirectoryOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? category, string? locality,
        string sort = "relevance", int page = 1, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        var pageSize = _options.PageSize;

        var query = _db.Advertisements
            .Where(a => a.Status == AdStatus.Approved)
            .Include(a => a.Category)
            .Include(a => a.Photos)
            .Include(a => a.ServiceOfferings)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(a =>
                EF.Functions.Like(a.BusinessName, "%" + term + "%") ||
                EF.Functions.Like(a.Description, "%" + term + "%") ||
                a.ServiceOfferings.Any(s => EF.Functions.Like(s.Name, "%" + term + "%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category!.Slug == category);

        if (!string.IsNullOrWhiteSpace(locality))
            query = query.Where(a => a.Locality == locality);

        query = sort switch
        {
            "newest" => query.OrderByDescending(a => a.CreatedUtc),
            "popular" => query.OrderByDescending(a => a.ViewCount),
            "name" => query.OrderBy(a => a.BusinessName),
            _ => query.OrderByDescending(a => a.IsFeatured).ThenByDescending(a => a.ViewCount)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var model = new DirectorySearchViewModel
        {
            Query = q,
            CategorySlug = category,
            Locality = locality,
            Sort = sort,
            Page = page,
            Results = new PagedResult<Advertisement>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            },
            Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct),
            Localities = await _db.Advertisements
                .Where(a => a.Status == AdStatus.Approved)
                .Select(a => a.Locality)
                .Distinct().OrderBy(l => l).ToListAsync(ct)
        };

        return View(model);
    }

    [HttpGet("listing/{id:int}/{slug?}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var ad = await _db.Advertisements
            .Include(a => a.Category)
            .Include(a => a.Photos.OrderByDescending(p => p.IsPrimary).ThenBy(p => p.SortOrder))
            .Include(a => a.ServiceOfferings.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(a => a.Id == id && a.Status == AdStatus.Approved, ct);

        if (ad is null) return NotFound();

        // Cheap view counter; a high-traffic build would push this to a queue.
        await _db.Advertisements.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);

        ViewData["Similar"] = await _db.Advertisements
            .Where(a => a.Status == AdStatus.Approved && a.CategoryId == ad.CategoryId && a.Id != ad.Id)
            .Include(a => a.Photos)
            .Include(a => a.Category)
            .Include(a => a.ServiceOfferings)
            .OrderByDescending(a => a.ViewCount)
            .Take(3)
            .ToListAsync(ct);

        return View(ad);
    }
}
