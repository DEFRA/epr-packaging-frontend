using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Privacy;

[ExcludeFromCodeCoverage]
public class PrivacyViewModel
{
    public string DataProtectionPublicRegisterUrl { get; set; }

    public string DefrasPersonalInformationCharterUrl { get; set; }

    public string InformationCommissionerUrl { get; set; }

    public string ScottishEnvironmentalProtectionAgencyUrl { get; set; }

    public string NationalResourcesWalesUrl { get; set; }

    public string NorthernIrelandEnvironmentAgencyUrl { get; set; }

    public string EnvironmentAgencyUrl { get; set; }

    public string DataProtectionEmail { get; set; }

    public string DefraGroupProtectionOfficerEmail { get; set; }

    public string LastUpdated { get; set; }
}