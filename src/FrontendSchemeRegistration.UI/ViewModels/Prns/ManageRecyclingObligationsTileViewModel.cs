using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

[ExcludeFromCodeCoverage]
public class ManageRecyclingObligationsTileViewModel
{
    public string ComplianceYear { get; set; } = string.Empty;

    public DateTime ObligationDeadline { get; set; }

    public CsocViewModel? CsocViewModel { get; set; }

    public bool IsComplianceScheme { get; set; }

    /// <summary>
    ///     The EPR Next Gen link when ShowPrnsOnCdp is on; null keeps the packaging page.
    /// </summary>
    public string? ManageObligationsHref { get; set; }
}
