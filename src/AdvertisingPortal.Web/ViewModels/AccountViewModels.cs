using System.ComponentModel.DataAnnotations;

namespace AdvertisingPortal.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }

    /// <summary>True when Google sign-in is configured, so the button can be shown.</summary>
    public bool GoogleEnabled { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = default!;

    /// <summary>True when Google sign-in is configured, so the button can be shown.</summary>
    public bool GoogleEnabled { get; set; }
}
