using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Organisation;

[ExcludeFromCodeCoverage]
public class OrganisationDto
{
    public int Id { get; set; }

    public Guid ExternalId { get; set; }

    public string? Name { get; set; }

    public string? TradingName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? CompaniesHouseNumber { get; set; }

    public string? ParentCompanyName { get; set; }
}