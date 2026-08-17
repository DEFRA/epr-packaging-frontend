namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class WebApiOptions
{
    public ObligationDataType ObligationData { get; set; }
    public ComplianceDeclarationStatusType ComplianceDeclarationStatus { get; set; }
    public PrnSearchDataType PrnSearchData { get; set; }
    public string ServiceRole { get; set; } = "Approved Person";

    public string PrnObligationCalculationResponseFile =>
        $"v1_prn_obligationcalculation_{ObligationData.ToString().ToLower()}.json";

    public string PrnSearchResponseFile => PrnSearchData switch
    {
        PrnSearchDataType.AllDecemberWasteOutsideFlashWindow => "v1_prn_search_all_december_waste_outside_flash_window.json",
        _ => "v1_prn_search.json"
    };

    public enum ObligationDataType
    {
        Mixed,
        NoDataYet
    }

    public enum PrnSearchDataType
    {
        Default,

        /// <summary>
        ///     All returned PRNs are editable, awaiting-acceptance, December Waste, but issued outside the
        ///     immediate Dec/Jan flash window - so checkboxes render (not the flash-only link).
        /// </summary>
        AllDecemberWasteOutsideFlashWindow
    }

    public enum ComplianceDeclarationStatusType
    {
        None,
        Submitted,
        Cancelled
    }
}