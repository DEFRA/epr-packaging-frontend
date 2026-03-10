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
    public LocalizedString Translate(AwaitingAcceptanceResultViewModel prn)
    {
        // PRN issue date is UTC stored in DB but offset is lost during
        // retrieval via PRN API
        var issueDate = DateTime.SpecifyKind(prn.DateIssued, DateTimeKind.Utc);
        
        return issueDate >= fibreOptions.Value.LaunchDateUtc
            ? prnDataResourcesPostFibre[prn.Material]
            : prnDataResources[prn.Material];
    }
}