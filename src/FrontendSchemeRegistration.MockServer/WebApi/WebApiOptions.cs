namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class WebApiOptions
{
    public ObligationDataType ObligationData { get; set; }
    public ComplianceDeclarationStatusType ComplianceDeclarationStatus { get; set; }
    public string ServiceRole { get; set; } = "Approved Person";

    public string PrnObligationCalculationResponseFile =>
        $"v1_prn_obligationcalculation_{ObligationData.ToString().ToLower()}.json";

    public enum ObligationDataType
    {
        Mixed,
        NoDataYet
    }

    public enum ComplianceDeclarationStatusType
    {
        None,
        Submitted,
        Cancelled
    }
}