using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YellowPages.Web.Data;
using YellowPages.Web.Models;
using YellowPages.Web.Options;
using YellowPages.Web.ViewModels;

namespace YellowPages.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly DirectoryOptions _directory;

    public HomeController(AppDbContext db, IOptions<DirectoryOptions> directory)
    {
        _db = db;
        _directory = directory.Value;
    }

    // No ResponseCache here. The layout renders the signed-in user's name and the
    // Admin link, so a shared/proxy-cacheable home page would serve a stale anonymous
    // navbar right after register/login and make sign-in look like it failed.
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var approved = _db.Advertisements.Where(a => a.Status == AdStatus.Approved);

        var model = new HomeViewModel
        {
            Categories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(ct),
            FeaturedAds = await approved
                .Where(a => a.IsFeatured)
                .Include(a => a.Category)
                .Include(a => a.Photos)
                .Include(a => a.ServiceOfferings)
                .OrderByDescending(a => a.ReviewedUtc)
                .Take(4)
                .ToListAsync(ct),
            RecentAds = await approved
                .Include(a => a.Category)
                .Include(a => a.Photos)
                .Include(a => a.ServiceOfferings)
                .OrderByDescending(a => a.CreatedUtc)
                .Take(8)
                .ToListAsync(ct),
            Localities = await approved
                .Select(a => a.Locality)
                .Distinct()
                .OrderBy(l => l)
                .Take(30)
                .ToListAsync(ct),
            TotalListings = await approved.CountAsync(ct)
        };

        ViewData["CityName"] = _directory.CityName;
        return View(model);
    }

    public IActionResult Privacy() => View();

    [Route("/error")]
    public IActionResult Error() => View();

    [Route("/denied")]
    public IActionResult Denied() => View();
}
