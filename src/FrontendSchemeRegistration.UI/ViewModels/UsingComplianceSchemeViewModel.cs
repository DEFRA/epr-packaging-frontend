namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Resources;

[ExcludeFromCodeCoverage]
public class UsingComplianceSchemeViewModel
{
    [Required(ErrorMessageResourceName = "using_compliance_scheme_selection_error", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool? UsingComplianceScheme { get; set; }

    public bool? SavedUsingComplianceScheme { get; set; }
}