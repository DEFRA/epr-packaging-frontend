using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember
{
    [ExcludeFromCodeCoverage]
    public record ComplianceSchemeMemberDetails
    {
        public string OrganisationName { get; set; }

        public string OrganisationNumber { get; set; }

        public string RegisteredNation { get; set; }

        public string ComplianceScheme { get; set; }

        public string? ProducerType { get; set; }

        public string? CompanyHouseNumber { get; set; }
    }
}
