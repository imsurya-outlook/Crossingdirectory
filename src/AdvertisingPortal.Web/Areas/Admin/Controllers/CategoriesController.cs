using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    public async Task<IActionResult> Index() => View(await _categories.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => View("Form", new TaxonomyFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxonomyFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var (ok, error) = await _categories.CreateAsync(model.Name, model.DisplayOrder, model.IsActive);
        if (!ok)
        {
            ModelState.AddModelError(nameof(model.Name), error!);
            return View("Form", model);
        }

        TempData["Success"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categories.GetByIdAsync(id);
        if (category is null) return NotFound();

        return View("Form", new TaxonomyFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaxonomyFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var (ok, error) = await _categories.UpdateAsync(model.Id, model.Name, model.DisplayOrder, model.IsActive);
        if (!ok)
        {
            ModelState.AddModelError(nameof(model.Name), error!);
            return View("Form", model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        await _categories.ToggleActiveAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, error) = await _categories.DeleteAsync(id);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Category removed.";

        return RedirectToAction(nameof(Index));
    }
}
