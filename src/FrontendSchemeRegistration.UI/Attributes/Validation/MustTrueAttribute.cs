namespace FrontendSchemeRegistration.UI.Attributes.Validation;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public class MustTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is true;
}