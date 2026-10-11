using System.ComponentModel.DataAnnotations;
using AdvertisingPortal.Application.Common;
using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdvertisingPortal.Web.ViewModels;

public class AdvertisementFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150), Display(Name = "Advertisement title")]
    public string Title { get; set; } = default!;

    [Required, StringLength(4000), Display(Name = "Description")]
    public string Description { get; set; } = default!;

    [Required, StringLength(100), Display(Name = "Contact name")]
    public string ContactName { get; set; } = default!;

    [Required, StringLength(20), Phone, Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = default!;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [Url, StringLength(250), Display(Name = "Website")]
    public string? WebsiteUrl { get; set; }

    [Required, StringLength(300), Display(Name = "Address")]
    public string AddressLine { get; set; } = default!;

    [Required, Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required, Display(Name = "Locality")]
    public int LocalityId { get; set; }

    [Display(Name = "Images")]
    public List<IFormFile> Images { get; set; } = new();

    public AdvertisementStatus Status { get; set; } = AdvertisementStatus.Draft;
    public List<AdvertisementImageDto> ExistingImages { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public List<SelectListItem> LocalityOptions { get; set; } = new();
    public bool IsEdit => Id > 0;
}

public class AdvertisementSearchViewModel
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? LocalityId { get; set; }
    public int Page { get; set; } = 1;

    public string? HeadingOverride { get; set; }
    public PagedResult<AdvertisementListItemDto> Results { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public List<SelectListItem> LocalityOptions { get; set; } = new();
}

public class HomeViewModel
{
    public List<AdvertisementListItemDto> Featured { get; set; } = new();
    public List<AdvertisementListItemDto> Recent { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<LocalityDto> Localities { get; set; } = new();
    public int? DefaultLocalityId { get; set; }
    public string DefaultLocalityName { get; set; } = "Crossing Republic";
}

public class AdvertiserDashboardViewModel
{
    public List<AdvertiserAdvertisementDto> Advertisements { get; set; } = new();
    public int TotalViews => Advertisements.Sum(a => a.ViewCount);
    public int TotalClicks => Advertisements.Sum(a => a.DetailsClickCount);
    public int LiveCount => Advertisements.Count(a => a.Status == AdvertisementStatus.Approved);
    public int PendingCount => Advertisements.Count(a => a.Status == AdvertisementStatus.PendingApproval);
}

public class AdminAdvertisementListViewModel
{
    public AdvertisementStatus? Status { get; set; }
    public List<AdminAdvertisementDto> Advertisements { get; set; } = new();
}

public class TaxonomyFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100), Display(Name = "Name")]
    public string Name { get; set; } = default!;

    [Display(Name = "Display order")]
    [Range(0, 1000)]
    public int DisplayOrder { get; set; } = 1;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id > 0;
}
