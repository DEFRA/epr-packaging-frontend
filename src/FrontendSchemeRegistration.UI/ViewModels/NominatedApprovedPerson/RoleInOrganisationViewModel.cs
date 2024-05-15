using System.ComponentModel.DataAnnotations;
using FrontendSchemeRegistration.UI.Enums;

namespace FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson
{
    public class RoleInOrganisationViewModel
    {
        [Required(ErrorMessage = "RoleInOrganisation.ErrorMessage")]
        public RoleInOrganisation? RoleInOrganisation { get; set; }

        public Guid Id { get; set; }
    }
}
