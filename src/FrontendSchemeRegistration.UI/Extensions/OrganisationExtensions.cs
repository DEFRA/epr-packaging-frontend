namespace FrontendSchemeRegistration.UI.Extensions;

using Constants;
using EPR.Common.Authorization.Models;

public static class OrganisationExtensions
{
    public static bool IsDirectProducer(this Organisation organisation) =>
        organisation.OrganisationRole == OrganisationRoles.Producer;
    
    public static bool IsComplianceScheme(this Organisation organisation) =>
        organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;
}