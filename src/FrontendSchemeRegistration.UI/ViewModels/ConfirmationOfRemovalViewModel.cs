using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class ConfirmationOfRemovalViewModel
{
    public string OrganisationName { get; set; }

    public Guid CurrentComplianceSchemeId { get; set; }
}
