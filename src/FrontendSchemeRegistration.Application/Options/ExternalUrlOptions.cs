namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ExternalUrlOptions
{
    public const string ConfigSection = "ExternalUrls";

    public string LandingPage { get; set; }

    public string GovUkHome { get; set; }

    public string PrivacyScottishEnvironmentalProtectionAgency { get; set; }

    public string PrivacyNationalResourcesWales { get; set; }

    public string PrivacyNorthernIrelandEnvironmentAgency { get; set; }

    public string PrivacyEnvironmentAgency { get; set; }

    public string PrivacyDataProtectionPublicRegister { get; set; }

    public string PrivacyDefrasPersonalInformationCharter { get; set; }

    public string PrivacyInformationCommissioner { get; set; }

    public string PrivacyPage { get; set; }

    public string AccessibilityPage { get; set; }

    public string FindAndUpdateCompanyInformation { get; set; }

    public string ProducerResponsibilityObligations { get; set; }

    public string ServiceNow { get; set; }

    public string FinancialServicesSupplier { get; set; }

    public string LearnMoreAboutPackUK { get; set; }
}