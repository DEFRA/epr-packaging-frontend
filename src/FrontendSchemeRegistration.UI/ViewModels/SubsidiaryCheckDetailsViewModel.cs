using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryCheckDetailsViewModel
{
    public string CompanyName { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public AddressViewModel? BusinessAddress { get; set; }
}