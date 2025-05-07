using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryCheckDetailsViewModel
{
    public string CompanyName { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public AddressViewModel? BusinessAddress { get; set; }

    public bool? IsCompanyAlreadyLinkedToTheParent { get; set; }

    public string? ParentCompanyName { get; set; }

    public string? ParentCompanyCompaniesHouseNumber { get; set; }

    public bool? IsCompanyAlreadyLinkedToOtherParent { get; set; }

    public string? OtherParentCompanyName { get; set; }    
}