using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

[ExcludeFromCodeCoverage]
public class ChooseAcceptanceYearViewModel
{
    public Guid ExternalId { get; set; }

    public bool IsPrn { get; set; }

    public int[] AvailableAcceptanceYears { get; set; } = [];

    [Required(ErrorMessage = "select_a_year")]
    public int? SelectedYear { get; set; }
}
