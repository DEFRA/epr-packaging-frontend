using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

[ExcludeFromCodeCoverage]
public class PrnObligationViewModel
{
    public string OrganisationRole { get; set; }
    public string OrganisationName { get; set; }
    public int? NationId { get; set; }
    public int CurrentYear { get; set; }
    public int DeadlineYear { get; set; }
}
