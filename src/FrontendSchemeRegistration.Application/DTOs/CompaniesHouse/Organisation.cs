using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;

[ExcludeFromCodeCoverage]
public record Organisation
{
    public string? Name { get; init; }

    public string? RegistrationNumber { get; init; }

    public RegisteredOfficeAddress? RegisteredOffice { get; init; }

    public OrganisationData? OrganisationData { get; init; }
}