namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class SiteDateOptions
{
    public const string ConfigSection = "SiteDates";

    public DateTime PrivacyLastUpdated { get; set; }

    public string DateFormat { get; set; }
}