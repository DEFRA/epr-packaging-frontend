using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryViewModel
{
    public SubsidiaryViewModel(int id, string name, string companiesHouseNumber)
    {
        Id = id;
        Name = name;
        CompaniesHouseNumber = companiesHouseNumber;
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public string CompaniesHouseNumber { get; set; }
}