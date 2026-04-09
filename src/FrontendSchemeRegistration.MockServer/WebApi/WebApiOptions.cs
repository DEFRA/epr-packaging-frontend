namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class WebApiOptions
{
    public ObligationDataType ObligationData { get; set; }

    public string PrnObligationCalculationResponseFile =>
        $"v1_prn_obligationcalculation_{ObligationData.ToString().ToLower()}.json";

    public enum ObligationDataType
    {
        Mixed,
        NoDataYet
    }
}