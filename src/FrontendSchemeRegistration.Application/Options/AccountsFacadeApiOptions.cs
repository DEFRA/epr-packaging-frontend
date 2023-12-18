namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class AccountsFacadeApiOptions
{
    public const string ConfigSection = "AccountsFacadeAPI";

    public string BaseEndpoint { get; set; }

    public string DownstreamScope { get; set; }
}
