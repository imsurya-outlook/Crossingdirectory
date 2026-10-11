using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Enums;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class AdvertisementsController : Controller
{
    private readonly IAdvertisementService _ads;

    public AdvertisementsController(IAdvertisementService ads) => _ads = ads;

    public async Task<IActionResult> Index(AdvertisementStatus? status)
        => View(new AdminAdvertisementListViewModel
        {
            Status = status,
            Advertisements = await _ads.GetAdminAdvertisementsAsync(status)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, AdvertisementStatus? status)
    {
        await _ads.SetStatusAsync(id, AdvertisementStatus.Approved, null);
        TempData["Success"] = "Advertisement approved.";
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? remarks, AdvertisementStatus? status)
    {
        await _ads.SetStatusAsync(id, AdvertisementStatus.Rejected, remarks);
        TempData["Success"] = "Advertisement rejected.";
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id, bool featured, AdvertisementStatus? status)
    {
        await _ads.SetFeaturedAsync(id, featured);
        TempData["Success"] = featured ? "Advertisement marked as featured." : "Advertisement removed from featured.";
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVerified(int id, bool verified, AdvertisementStatus? status)
    {
        await _ads.SetVerifiedAsync(id, verified);
        TempData["Success"] = verified
            ? "Advertisement marked as verified. The star badge is now visible to users."
            : "Verified badge removed.";

        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool active, AdvertisementStatus? status)
    {
        await _ads.SetActiveAsync(id, active);
        TempData["Success"] = active ? "Advertisement activated." : "Advertisement deactivated.";
        return RedirectToAction(nameof(Index), new { status });
    }
}
