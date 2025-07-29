using FrontendSchemeRegistration.Application.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public abstract class OrganisationNationViewModel
{
    public string RegulatorNation { get; set; }

    // full nation name
    public string NationName => NationExtensions.GetNationName(RegulatorNation);

    public string EnvironmentAgency => NationExtensions.GetEnvironmentAgencyName(RegulatorNation);
}