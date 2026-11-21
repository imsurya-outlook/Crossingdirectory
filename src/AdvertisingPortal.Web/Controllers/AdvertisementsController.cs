using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace AdvertisingPortal.Web.Controllers;

[Route("ads")]
public class AdvertisementsController : Controller
{
    private readonly IAdvertisementService _ads;
    private readonly ICategoryService _categories;
    private readonly ILocalityService _localities;
    private readonly IAnalyticsService _analytics;
    private readonly IRatingService _ratings;

    public AdvertisementsController(
        IAdvertisementService ads,
        ICategoryService categories,
        ILocalityService localities,
        IAnalyticsService analytics,
        IRatingService ratings)
    {
        _ads = ads;
        _categories = categories;
        _localities = localities;
        _analytics = analytics;
        _ratings = ratings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? keyword, int? categoryId, int? localityId, int page = 1)
    {
        localityId ??= await _localities.GetDefaultLocalityIdAsync();

        var model = await BuildSearchModelAsync(keyword, categoryId, localityId, page, null);
        return View(model);
    }

    [HttpGet("/category/{slug}")]
    public async Task<IActionResult> ByCategory(string slug, string? keyword, int? localityId, int page = 1)
    {
        var categoryId = await _categories.GetIdBySlugAsync(slug);
        if (categoryId is null) return NotFound();

        var categories = await _categories.GetActiveAsync();
        var heading = categories.FirstOrDefault(c => c.Id == categoryId)?.Name;

        var model = await BuildSearchModelAsync(keyword, categoryId, localityId, page, heading);
        return View(nameof(Index), model);
    }

    [HttpGet("/locality/{slug}")]
    public async Task<IActionResult> ByLocality(string slug, string? keyword, int? categoryId, int page = 1)
    {
        var localityId = await _localities.GetIdBySlugAsync(slug);
        if (localityId is null) return NotFound();

        var localities = await _localities.GetActiveAsync();
        var heading = localities.FirstOrDefault(l => l.Id == localityId)?.Name;

        var model = await BuildSearchModelAsync(keyword, categoryId, localityId, page, heading);
        return View(nameof(Index), model);
    }

    [HttpGet("click/{id:int}")]
    public async Task<IActionResult> Click(int id)
    {
        var slug = await _analytics.RegisterDetailsClickAndGetSlugAsync(id);
        if (slug is null) return NotFound();

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var model = await _ads.GetPublicDetailsBySlugAsync(slug);
        if (model is null) return NotFound();

        // Counted once per visitor per ad inside the de-duplication window, so a
        // refresh no longer increments the counter.
        var counted = await _analytics.TryIncrementViewAsync(model.Id, ResolveViewerKey());
        if (counted) model.ViewCount += 1;

        if (User.Identity?.IsAuthenticated == true)
            model.MyScore = await _ratings.GetUserScoreAsync(model.Id, User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        return View(model);
    }

    /// <summary>
    /// Stable identity for view de-duplication: the signed-in account when there
    /// is one, otherwise a durable anonymous visitor cookie.
    /// </summary>
    private string ResolveViewerKey()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId)) return "u:" + userId;

        const string cookieName = "cd_visitor";
        if (Request.Cookies.TryGetValue(cookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
            return "a:" + existing;

        var visitorId = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(cookieName, visitorId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        return "a:" + visitorId;
    }


    [HttpPost("{slug}/rate")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(string slug, int score, string? comment)
    {
        var ad = await _ads.GetPublicDetailsBySlugAsync(slug);
        if (ad is null) return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var (ok, error) = await _ratings.SubmitAsync(ad.Id, userId, score, comment);

        if (ok) TempData["Success"] = "Thanks for rating this business.";
        else TempData["Error"] = error;

        return RedirectToAction(nameof(Details), new { slug });
    }

    private async Task<AdvertisementSearchViewModel> BuildSearchModelAsync(
        string? keyword, int? categoryId, int? localityId, int page, string? heading)
    {
        var results = await _ads.SearchAsync(new AdvertisementSearchDto
        {
            Keyword = keyword,
            CategoryId = categoryId,
            LocalityId = localityId,
            Page = page,
            PageSize = 9
        });

        return new AdvertisementSearchViewModel
        {
            Keyword = keyword,
            CategoryId = categoryId,
            LocalityId = localityId,
            Page = page,
            HeadingOverride = heading,
            Results = results,
            CategoryOptions = (await _categories.GetActiveAsync())
                .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == categoryId)).ToList(),
            LocalityOptions = (await _localities.GetActiveAsync())
                .Select(l => new SelectListItem(l.Name, l.Id.ToString(), l.Id == localityId)).ToList()
        };
    }
}
