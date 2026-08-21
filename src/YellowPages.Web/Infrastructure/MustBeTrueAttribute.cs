using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace YellowPages.Web.ViewModels;

/// <summary>
/// Requires a bool to be true — the "you must accept the terms" checkbox.
/// Deliberately NOT [Range(typeof(bool), "true", "true")]: that emits
/// data-val-range-min="True", jQuery Validation's range rule parses it as a number,
/// gets NaN, and fails the checkbox even when it is ticked — which silently prevents
/// the form from ever submitting. This attribute emits a client rule jQuery
/// understands ("required" on a checkbox) and validates server-side too.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public MustBeTrueAttribute()
        : base("This box must be ticked to continue.") { }

    public override bool IsValid(object? value) => value is true;

    public void AddValidation(ClientModelValidationContext context)
    {
        var message = FormatErrorMessage(context.ModelMetadata.GetDisplayName() ?? string.Empty);
        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-required", message);
    }
}
