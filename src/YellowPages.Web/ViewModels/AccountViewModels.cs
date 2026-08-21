using System.ComponentModel.DataAnnotations;

namespace YellowPages.Web.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(120, MinimumLength = 3)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Enter a valid mobile number (10-15 digits, optional leading +).")]
    [Display(Name = "Mobile number")]
    public string MobileNumber { get; set; } = string.Empty;

    [Display(Name = "Locality / sector")]
    [StringLength(120)]
    public string? Locality { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // A bool cannot use [Range]: jQuery Validation's range rule is numeric, so
    // data-val-range-min="True" becomes NaN and the checkbox fails client-side
    // validation even when ticked — silently blocking the whole form.
    [MustBeTrue(ErrorMessage = "You must accept the terms to register.")]
    [Display(Name = "I accept the terms of use")]
    public bool AcceptTerms { get; set; }
}

public class LoginViewModel
{
    [Required]
    [Display(Name = "Email or mobile number")]
    public string Identifier { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
