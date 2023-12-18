using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

[ExcludeFromCodeCoverage]
public class ProducerComplianceSchemeDto
{
    public Guid SelectedSchemeId { get; set; }

    public Guid ComplianceSchemeId { get; set; }

    public string ComplianceSchemeName { get; set; }

    public string? ComplianceSchemeOperatorName { get; set; }

    public Guid? ComplianceSchemeOperatorId { get; set; }
}