namespace FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeReasonsRemovalDto
{
    public string Code { get; set; }

    public bool RequiresReason { get; set; }
}