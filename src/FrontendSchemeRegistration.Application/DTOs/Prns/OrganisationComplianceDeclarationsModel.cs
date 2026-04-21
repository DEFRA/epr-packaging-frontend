using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Prns;

public class OrganisationComplianceDeclarationsModel
{
    public IEnumerable<ComplianceDeclarationModel> ComplianceDeclarations { get; init; } = [];
}

public class ComplianceDeclarationModel
{
    public ComplianceDeclarationStatus Status { get; init; }
}
