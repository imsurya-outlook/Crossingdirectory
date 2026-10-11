using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Areas.Advertiser.Controllers;

[Area("Advertiser")]
[Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
public class DashboardController : Controller
{
    private readonly IAdvertisementService _ads;

    public DashboardController(IAdvertisementService ads) => _ads = ads;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

        return View(new AdvertiserDashboardViewModel
        {
            Advertisements = await _ads.GetAdvertiserAdvertisementsAsync(userId)
        });
    }
}
