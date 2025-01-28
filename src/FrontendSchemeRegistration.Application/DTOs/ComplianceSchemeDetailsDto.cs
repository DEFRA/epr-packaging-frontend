using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeDetailsDto
{
    public List<ComplianceSchemeDetailsMemberDto> Members { get; set; }
}