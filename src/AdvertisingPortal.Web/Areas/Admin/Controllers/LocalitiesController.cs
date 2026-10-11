using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class LocalitiesController : Controller
{
    private readonly ILocalityService _localities;

    public LocalitiesController(ILocalityService localities) => _localities = localities;

    public async Task<IActionResult> Index() => View(await _localities.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => View("Form", new TaxonomyFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxonomyFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var (ok, error) = await _localities.CreateAsync(model.Name, model.DisplayOrder, model.IsActive);
        if (!ok)
        {
            ModelState.AddModelError(nameof(model.Name), error!);
            return View("Form", model);
        }

        TempData["Success"] = "Locality created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var locality = await _localities.GetByIdAsync(id);
        if (locality is null) return NotFound();

        return View("Form", new TaxonomyFormViewModel
        {
            Id = locality.Id,
            Name = locality.Name,
            DisplayOrder = locality.DisplayOrder,
            IsActive = locality.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaxonomyFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var (ok, error) = await _localities.UpdateAsync(model.Id, model.Name, model.DisplayOrder, model.IsActive);
        if (!ok)
        {
            ModelState.AddModelError(nameof(model.Name), error!);
            return View("Form", model);
        }

        TempData["Success"] = "Locality updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        await _localities.ToggleActiveAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, error) = await _localities.DeleteAsync(id);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Locality removed.";

        return RedirectToAction(nameof(Index));
    }
}
