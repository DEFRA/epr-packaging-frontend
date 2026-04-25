namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class WebApiOptions
{
    public const string ConfigSection = "WebAPI";

    public string BaseEndpoint { get; set; }

    public string DownstreamScope { get; set; }
}
