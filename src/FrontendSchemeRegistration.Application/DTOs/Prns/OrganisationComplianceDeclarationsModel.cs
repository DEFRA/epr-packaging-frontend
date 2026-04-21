using FrontendSchemeRegistration.Application.Enums;
using System.Text.Json.Serialization;

namespace FrontendSchemeRegistration.Application.DTOs.Prns;

public class OrganisationComplianceDeclarationsModel
{
    public IEnumerable<ComplianceDeclarationModel> ComplianceDeclarations { get; init; } = [];
}

public class ComplianceDeclarationModel
{
    public DateTimeOffset Created { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ComplianceDeclarationStatus Status { get; init; }
}
