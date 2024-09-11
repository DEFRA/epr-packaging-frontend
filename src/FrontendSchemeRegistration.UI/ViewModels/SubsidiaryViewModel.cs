using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryViewModel
{
    public SubsidiaryViewModel(string id, string name, string companiesHouseNumber)
    {
        Id = id;
        Name = name;
        CompaniesHouseNumber = companiesHouseNumber;
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string CompaniesHouseNumber { get; set; }
}