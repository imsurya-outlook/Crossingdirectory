using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Controllers;

public class HomeController : Controller
{
    private readonly IAdvertisementService _ads;
    private readonly ICategoryService _categories;
    private readonly ILocalityService _localities;

    public HomeController(IAdvertisementService ads, ICategoryService categories, ILocalityService localities)
    {
        _ads = ads;
        _categories = categories;
        _localities = localities;
    }

    public async Task<IActionResult> Index()
    {
        var localities = await _localities.GetActiveAsync();
        var defaultId = await _localities.GetDefaultLocalityIdAsync();

        var model = new HomeViewModel
        {
            Featured = await _ads.GetFeaturedAsync(6),
            Recent = await _ads.GetRecentAsync(6),
            Categories = await _categories.GetActiveAsync(),
            Localities = localities,
            DefaultLocalityId = defaultId,
            DefaultLocalityName = localities.FirstOrDefault(l => l.Id == defaultId)?.Name ?? "Crossing Republic"
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
