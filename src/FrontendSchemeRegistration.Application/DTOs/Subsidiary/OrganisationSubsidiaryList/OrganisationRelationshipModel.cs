using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;

[ExcludeFromCodeCoverage]
public class OrganisationRelationshipModel
{
    public OrganisationDetailModel? Organisation { get; set; }

    public List<RelationshipResponseModel> Relationships { get; set; } = new();
}