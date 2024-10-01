using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class SubsidiaryOrganisationViewModel
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public List<SubsidiaryViewModel> Subsidiaries { get; set; }
    
    public Guid ExternalId { get; set; }
}