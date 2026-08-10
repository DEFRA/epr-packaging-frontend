using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

[ExcludeFromCodeCoverage]
public class ChooseYearViewModel
{
    public int? SelectedYear { get; set; }

    public int CurrentYear { get; set; }

    public IReadOnlyList<int> Years { get; set; } = [];
}
