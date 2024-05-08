namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Attributes.Validation;

[ExcludeFromCodeCoverage]
public class TelephoneNumberViewModel
{
    public Guid? EnrolmentId { get; set; }

    [TelephoneNumberValidation(ErrorMessage = "TelephoneNumber.Question.ErrorMessage")]
    public string? TelephoneNumber { get; set; }

    public string? EmailAddress { get; set; }
}