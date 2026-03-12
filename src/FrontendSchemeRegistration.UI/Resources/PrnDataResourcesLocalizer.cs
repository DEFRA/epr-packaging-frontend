namespace FrontendSchemeRegistration.UI.Resources;

using Application.Options;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using ViewModels.Prns;

public class PrnDataResourcesLocalizer(
    IStringLocalizer<PrnDataResources> prnDataResources,
    IStringLocalizer<PrnDataResourcesPostFibre> prnDataResourcesPostFibre,
    IOptions<FibreOptions> fibreOptions)
{
    public LocalizedString Translate(AwaitingAcceptanceResultViewModel prn) =>
        prn.DateIssued >= fibreOptions.Value.LaunchDateUtc
            ? prnDataResourcesPostFibre[prn.Material]
            : prnDataResources[prn.Material];
}