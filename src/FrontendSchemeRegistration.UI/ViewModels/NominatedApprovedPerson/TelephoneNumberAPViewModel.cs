using FrontendSchemeRegistration.UI.Attributes.Validation;

namespace FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson
{
    public class TelephoneNumberAPViewModel
    {
        [TelephoneNumberValidation(ErrorMessage = "TelephoneNumber.Question.ErrorMessage")]
        public string? TelephoneNumber { get; set; }

        public string? EmailAddress { get; set; }

        public Guid Id { get; set; }
    }
}
