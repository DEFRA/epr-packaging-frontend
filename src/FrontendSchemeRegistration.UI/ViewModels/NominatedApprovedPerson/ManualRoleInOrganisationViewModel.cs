using System.ComponentModel.DataAnnotations;

namespace FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson
{
    public class ManualRoleInOrganisationViewModel
    {
        [MaxLength(450, ErrorMessage = "ManualRoleInOrganisation.LengthErrorMessage")]
        [Required(ErrorMessage = "ManualRoleInOrganisation.ErrorMessage")]
        public string? RoleInOrganisation { get; set; }

        public Guid Id { get; set; }
    }
}
