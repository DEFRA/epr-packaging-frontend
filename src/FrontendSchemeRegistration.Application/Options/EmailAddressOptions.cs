namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class EmailAddressOptions
{
    public const string ConfigSection = "EmailAddresses";

    public string DataProtection { get; set; }

    public string DefraGroupProtectionOfficer { get; set; }
}
