using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

// for rendering partial view Views\Shared\Partials\Prns\_prnMaterialTable.cshtml
[ExcludeFromCodeCoverage]
public class PrnMaterialObligationTableViewModel
{
    public string TableCaption { get; set; }

    public bool ShowMaterialsAsHyperlink { get; set; }

    public List<PrnMaterialObligationViewModel> PrnMaterialObligationViewModels { get; set; }
}
