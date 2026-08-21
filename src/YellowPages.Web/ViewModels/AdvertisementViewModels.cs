using System.ComponentModel.DataAnnotations;
using YellowPages.Web.Models;

namespace YellowPages.Web.ViewModels;

public class ServiceOfferingInput
{
    [Required, StringLength(120)]
    [Display(Name = "Service")]
    public string Name { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Details { get; set; }

    [Range(0, 10_000_000)]
    [Display(Name = "Price from")]
    public decimal? PriceFrom { get; set; }

    [StringLength(40)]
    [Display(Name = "Unit")]
    public string? PriceUnit { get; set; }
}

public class AdvertisementFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(140, MinimumLength = 3)]
    [Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required, StringLength(4000, MinimumLength = 40)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(250)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "Locality / sector")]
    public string Locality { get; set; } = string.Empty;

    [Required, StringLength(120)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [StringLength(12)]
    [Display(Name = "PIN code")]
    public string? PostalCode { get; set; }

    [Required]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Enter a valid contact number.")]
    [Display(Name = "Contact number")]
    public string ContactPhone { get; set; } = string.Empty;

    [EmailAddress, StringLength(180)]
    [Display(Name = "Contact email")]
    public string? ContactEmail { get; set; }

    [Url, StringLength(250)]
    [Display(Name = "Website")]
    public string? WebsiteUrl { get; set; }

    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Enter a valid WhatsApp number.")]
    [Display(Name = "WhatsApp number")]
    public string? WhatsAppNumber { get; set; }

    [StringLength(180)]
    [Display(Name = "Opening hours")]
    public string? OpeningHours { get; set; }

    [MinLength(1, ErrorMessage = "Add at least one service offering.")]
    [Display(Name = "Services offered")]
    public List<ServiceOfferingInput> Services { get; set; } = new() { new ServiceOfferingInput() };

    [Display(Name = "Photos")]
    public List<IFormFile> NewPhotos { get; set; } = new();

    public List<AdPhoto> ExistingPhotos { get; set; } = new();
    public List<int> RemovePhotoIds { get; set; } = new();

    public AdStatus Status { get; set; }
    public string? ModerationNote { get; set; }
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
}
