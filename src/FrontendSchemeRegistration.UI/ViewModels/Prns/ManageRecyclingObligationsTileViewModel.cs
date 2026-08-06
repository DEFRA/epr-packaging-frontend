using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

[ExcludeFromCodeCoverage]
public class ManageRecyclingObligationsTileViewModel
{
    public string ComplianceYear { get; set; } = string.Empty;

    public DateTime ObligationDeadline { get; set; }

    public CsocViewModel? CsocViewModel { get; set; }

    public bool IsComplianceScheme { get; set; }
}
