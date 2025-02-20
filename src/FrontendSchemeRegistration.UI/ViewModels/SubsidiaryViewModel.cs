using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryViewModel
{
    public SubsidiaryViewModel(string id, string name, string companiesHouseNumber, DateTime? joinerDate, string reportingType)
    {
        Id = id;
        Name = name;
        CompaniesHouseNumber = companiesHouseNumber;
        JoinerDate = joinerDate;
        ReportingType = reportingType;
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public DateTime? JoinerDate { get; set; }

    public string ReportingType { get; set; }
}