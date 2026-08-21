namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

public class ChooseYearViewModel
{
    public int CsocUnavailablePastYear { get; set; } = 2025;

    public int? SelectedYear { get; set; }

    public int CurrentYear { get; set; }

    public IReadOnlyList<int> Years { get; set; } = [];
}
