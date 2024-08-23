using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;

[ExcludeFromCodeCoverage]
public class RelationshipResponseModel
{
    public string OrganisationNumber { get; set; }

    public string OrganisationName { get; set; }

    public string RelationshipType { get; set; }
}