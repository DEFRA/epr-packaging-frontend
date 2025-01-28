using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class ConfirmRemoveSubsidiarySuccessViewModel
{
    public string SubsidiaryName { get; set; }

    public int? ReturnToSubsidiaryPage { get; set; }
}