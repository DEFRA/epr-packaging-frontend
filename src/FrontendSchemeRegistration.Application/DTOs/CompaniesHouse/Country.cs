using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;

[ExcludeFromCodeCoverage]
public record Country
{
    public string? Name { get; init; }

    public string? Iso { get; init; }
}
