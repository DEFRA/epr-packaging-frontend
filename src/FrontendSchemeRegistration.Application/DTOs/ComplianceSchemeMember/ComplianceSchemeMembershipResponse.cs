using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember
{
        public class ComplianceSchemeMembershipResponse
    {
        public PaginatedResponse<ComplianceSchemeMemberDto> PagedResult { get; set; }

        public string SchemeName { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public int LinkedOrganisationCount { get; set; }

        public int SubsidiariesCount { get; set; }
    }
}
