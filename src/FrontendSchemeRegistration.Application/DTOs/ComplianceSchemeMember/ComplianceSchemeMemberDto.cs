using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember
{
    [ExcludeFromCodeCoverage]
    public class ComplianceSchemeMemberDto
    {
        public Guid SelectedSchemeId { get; set; }

        public string OrganisationNumber { get; set; }

        public string OrganisationName { get; set; }
    }
}
