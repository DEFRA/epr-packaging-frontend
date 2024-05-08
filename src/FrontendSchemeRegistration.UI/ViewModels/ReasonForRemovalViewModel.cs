using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class ReasonForRemovalViewModel
{
    public IReadOnlyCollection<ComplianceSchemeReasonsRemovalDto> ReasonForRemoval { get; set; }

    public string OrganisationName { get; set; }

    [Required(ErrorMessage = "ReasonForRemoval.Error")]
    public string SelectedReasonForRemoval { get; set; }

    public bool IsApprovedUser { get; set; }
}
