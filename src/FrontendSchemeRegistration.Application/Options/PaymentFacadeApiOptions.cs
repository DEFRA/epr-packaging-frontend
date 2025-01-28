namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class PaymentFacadeApiOptions
{
    public const string ConfigSection = "PaymentFacadeApi";

    public string BaseUrl { get; set; }

    public string DownstreamScope { get; set; }

    public PaymentFacadeApiEndpoints Endpoints { get; set; }
}
