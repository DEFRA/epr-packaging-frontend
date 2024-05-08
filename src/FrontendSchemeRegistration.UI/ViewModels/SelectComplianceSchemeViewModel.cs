namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Application.DTOs.ComplianceScheme;
using Resources;

[ExcludeFromCodeCoverage]
public class SelectComplianceSchemeViewModel
{
    public List<ComplianceSchemeDto> ComplianceSchemes { get; set; } = new();

    [Required(ErrorMessageResourceName = "select_which_compliance_error", ErrorMessageResourceType = typeof(ErrorMessages))]
    public string SelectedComplianceSchemeValues { get; set; }

    public string? SavedComplianceScheme { get; set; }
}