using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceScheme
{
    [ExcludeFromCodeCoverage]
    public class SmallProducerLinkActiveDurationSettings
    {
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
    }
}