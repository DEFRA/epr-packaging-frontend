namespace FrontendSchemeRegistration.UI.Attributes.Validation;

using System.ComponentModel.DataAnnotations;
using Application.Validations;

public class TelephoneNumberValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var phoneNumber = value?.ToString() ?? string.Empty;

        return TelephoneNumberValidator.IsValid(phoneNumber) ? ValidationResult.Success : new ValidationResult(ErrorMessage);
    }
}