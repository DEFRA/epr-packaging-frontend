using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

[ExcludeFromCodeCoverage]
public record ComplianceSchemeSummary
{
    public string Name { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Nation? Nation { get; init; }

    public DateTimeOffset CreatedOn { get; init; }

    public DateTimeOffset? MembersLastUpdatedOn { get; init; }

    public int MemberCount { get; init; }
}
