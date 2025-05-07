using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using FrontendSchemeRegistration.Application.DTOs.Addresses;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryConfirmCompanyDetailsViewModel
{
    public string CompanyName { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public Address? BusinessAddress { get; set; }

    public string? OrganisationId { get; set; }

    public bool? IsCompanyAlreadyLinkedToTheParent { get; set; }

    public string? ParentCompanyName { get; set; }

    public string? ParentCompanyCompaniesHouseNumber { get; set; }

    public bool? IsCompanyAlreadyLinkedToOtherParent { get; set; }

    public string? OtherParentCompanyName { get; set; }
}
