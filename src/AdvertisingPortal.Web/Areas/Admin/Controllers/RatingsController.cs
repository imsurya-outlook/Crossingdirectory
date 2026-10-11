using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class RatingsController : Controller
{
    private readonly IRatingService _ratings;

    public RatingsController(IRatingService ratings) => _ratings = ratings;

    public async Task<IActionResult> Index(int? advertisementId)
    {
        ViewData["AdvertisementId"] = advertisementId;
        return View(await _ratings.GetAllAsync(advertisementId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? advertisementId)
    {
        await _ratings.DeleteAsync(id);
        TempData["Success"] = "Rating removed and the average recalculated.";
        return RedirectToAction(nameof(Index), new { advertisementId });
    }
}
