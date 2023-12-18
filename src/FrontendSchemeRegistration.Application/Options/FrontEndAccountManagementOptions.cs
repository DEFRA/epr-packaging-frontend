namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FrontEndAccountManagementOptions
{
    public const string ConfigSection = "FrontEndAccountManagement";

    public string BaseUrl { get; set; }
}