namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Enums;
using Resources;

[ExcludeFromCodeCoverage]
public class ChangeComplianceSchemeOptionsViewModel
{
    [Required(ErrorMessageResourceName = "select_whether_youve_changed_compliance_scheme", ErrorMessageResourceType = typeof(ErrorMessages))]
    public ChangeComplianceSchemeOptions? ChangeComplianceSchemeOptions { get; set; }

    public ChangeComplianceSchemeOptions? SavedChangeComplianceSchemeOptions { get; set; }
}