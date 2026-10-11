using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class DashboardController : Controller
{
    private readonly IAdvertisementService _ads;

    public DashboardController(IAdvertisementService ads) => _ads = ads;

    public async Task<IActionResult> Index() => View(await _ads.GetAdminDashboardAsync());
}
