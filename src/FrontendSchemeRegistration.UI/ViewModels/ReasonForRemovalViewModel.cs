using System.ComponentModel.DataAnnotations;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

namespace FrontendSchemeRegistration.UI.ViewModels;

public class ReasonForRemovalViewModel
{
    public IReadOnlyCollection<ComplianceSchemeReasonsRemovalDto> ReasonForRemoval { get; set; }

    public string OrganisationName { get; set; }

    [Required(ErrorMessage = "ReasonForRemoval.Error")]
    public string SelectedReasonForRemoval { get; set; }

    public bool IsApprovedUser { get; set; }
}
