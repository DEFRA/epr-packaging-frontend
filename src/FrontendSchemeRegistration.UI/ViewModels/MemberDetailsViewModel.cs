namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class MemberDetailsViewModel
{
    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public string RegisteredNation { get; set; }

    public string ComplianceScheme { get; set; }

    public string? OrganisationType { get; set; }

    public string? CompanyHouseNumber { get; set; }

    public Guid SelectedSchemeId { get; set; }

    public bool ShowRemoveLink { get; set; }
}