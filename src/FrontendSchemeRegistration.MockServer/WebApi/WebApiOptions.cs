namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class WebApiOptions
{
    public string PrnObligationCalculationResponseFile { get; set; } = "v1_prn_obligationcalculation.json";
}