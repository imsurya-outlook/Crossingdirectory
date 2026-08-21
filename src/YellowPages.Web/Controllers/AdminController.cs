using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YellowPages.Web.Data;
using YellowPages.Web.Models;
using YellowPages.Web.ViewModels;

namespace YellowPages.Web.Controllers;

/// <summary>
/// Administrator module: lists every advertisement in the directory and drives
/// the moderation workflow (approve / reject / suspend / feature).
/// </summary>
[Authorize(Policy = "AdminOnly")]
[Route("admin")]
public class AdminController : Controller
{
    private const int PageSize = 20;
    private readonly AppDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Dashboard() => RedirectToAction(nameof(Ads));

    [HttpGet("ads")]
    public async Task<IActionResult> Ads(string? q, AdStatus? status, int page = 1, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;

        var query = _db.Advertisements
            .Include(a => a.Category)
            .Include(a => a.Owner)
            .Include(a => a.Photos)
            .Include(a => a.ServiceOfferings)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(a =>
                EF.Functions.Like(a.BusinessName, "%" + term + "%") ||
                EF.Functions.Like(a.Locality, "%" + term + "%") ||
                EF.Functions.Like(a.Owner!.Email!, "%" + term + "%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.Status == AdStatus.Pending ? 0 : 1)
            .ThenByDescending(a => a.CreatedUtc)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        var model = new AdminAdListViewModel
        {
            Query = q,
            Status = status,
            Page = page,
            Results = new PagedResult<Advertisement>
            {
                Items = items,
                Page = page,
                PageSize = PageSize,
                TotalCount = total
            },
            PendingCount = await _db.Advertisements.CountAsync(a => a.Status == AdStatus.Pending, ct),
            ApprovedCount = await _db.Advertisements.CountAsync(a => a.Status == AdStatus.Approved, ct),
            RejectedCount = await _db.Advertisements.CountAsync(a => a.Status == AdStatus.Rejected, ct),
            SuspendedCount = await _db.Advertisements.CountAsync(a => a.Status == AdStatus.Suspended, ct)
        };

        return View(model);
    }

    [HttpGet("ads/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var ad = await _db.Advertisements
            .Include(a => a.Category)
            .Include(a => a.Owner)
            .Include(a => a.Photos.OrderBy(p => p.SortOrder))
            .Include(a => a.ServiceOfferings.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return ad is null ? NotFound() : View(ad);
    }

    [HttpPost("ads/{id:int}/moderate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Moderate(int id, AdStatus status, string? note, CancellationToken ct)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (ad is null) return NotFound();

        ad.Status = status;
        ad.ModerationNote = note;
        ad.ReviewedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Ad {AdId} moved to {Status} by admin", id, status);
        TempData["Toast"] = $"\"{ad.BusinessName}\" is now {status}.";
        return RedirectToAction(nameof(Ads));
    }

    [HttpPost("ads/{id:int}/feature")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeature(int id, CancellationToken ct)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (ad is null) return NotFound();

        ad.IsFeatured = !ad.IsFeatured;
        await _db.SaveChangesAsync(ct);

        TempData["Toast"] = ad.IsFeatured
            ? $"\"{ad.BusinessName}\" is now featured on the home page."
            : $"\"{ad.BusinessName}\" is no longer featured.";
        return RedirectToAction(nameof(Ads));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct)
        => View(await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(ct));
}
