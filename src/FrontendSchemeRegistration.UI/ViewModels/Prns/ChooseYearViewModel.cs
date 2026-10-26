namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

using FrontendSchemeRegistration.Application.Extensions;

public class ChooseYearViewModel
{
    public int CsocUnavailablePastYear { get; set; } = ObligationYearOptions.CsocUnavailablePastYear;

    public int? SelectedYear { get; set; }

    public int CurrentYear { get; set; }

    public IReadOnlyList<int> Years { get; set; } = [];
}
