using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;

[ExcludeFromCodeCoverage]
public record OrganisationData
{
    public DateTime? DateOfCreation { get; init; }

    public string? Status { get; init; }

    public string? Type { get; init; }
}
