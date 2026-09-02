namespace FrontendSchemeRegistration.UI.ViewModels;

using Constants;

public class ViewModelWithOrganisationRole
{
    public string? OrganisationRole { get; set; }

    public int? OrganisationNationId { get; set; }

    public bool IsComplianceScheme => OrganisationRole == OrganisationRoles.ComplianceScheme;
}
