using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Addresses;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryConfirmCompanyDetailsViewModel
{
    public string CompanyName { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public Address? BusinessAddress { get; set; }

    public string? OrganisationId { get; set; }
}
