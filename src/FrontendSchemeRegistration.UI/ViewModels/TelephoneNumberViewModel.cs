namespace FrontendSchemeRegistration.UI.ViewModels;

using Attributes.Validation;

public class TelephoneNumberViewModel
{
    public Guid? EnrolmentId { get; set; }

    [TelephoneNumberValidation(ErrorMessage = "TelephoneNumber.Question.ErrorMessage")]
    public string? TelephoneNumber { get; set; }

    public string? EmailAddress { get; set; }
}