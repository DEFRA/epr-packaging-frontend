using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember
{
    [ExcludeFromCodeCoverage]
    public class ComplianceSchemeMemberDto
    {
        public Guid SelectedSchemeId { get; set; }

        public string? OrganisationNumber { get; set; }

        public string? OrganisationName { get; set; }

        public List<RelationshipResponseModel> Relationships { get; set; } = new();
    }
}