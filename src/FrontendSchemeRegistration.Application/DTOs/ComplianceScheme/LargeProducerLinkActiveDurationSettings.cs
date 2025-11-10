using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceScheme
{
    [ExcludeFromCodeCoverage]
    public class LargeProducerLinkActiveDurationSettings
    {
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
    }
}

