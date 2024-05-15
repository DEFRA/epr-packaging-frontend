using System.ComponentModel.DataAnnotations;

namespace FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson
{
    public class DeclarationApprovedPersonViewModel
    {
        [MaxLength(200, ErrorMessage = "Declaration.FullNameMaxLengthError")]
        [Required(ErrorMessage = "Declaration.FullNameError")]
        public string? DeclarationFullName { get; set; }

        public string? OrganisationName { get; set; }

        public Guid Id { get; set; }
    }
}
