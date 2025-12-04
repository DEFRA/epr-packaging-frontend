using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Mvc;
using System.Security.Policy;

namespace FrontendSchemeRegistration.UI.Extensions;

using Sessions;

public static class BackLinkExtensions
{
    public static string AppendBackLink(this string basepath, bool isResubmission, int? registrationYear = null, RegistrationJourney? registrationJourney = null)
    {
        var queryParams = new Dictionary<string, string>();

        if (isResubmission)
            queryParams["isResubmission"] = "true";

        if (registrationYear.HasValue)
            queryParams["registrationyear"] = registrationYear.Value.ToString();
        
        if(registrationJourney.HasValue)
            queryParams["registrationjourney"] = registrationJourney.Value.ToString();

        return basepath.AppendResubmissionFlagToQueryString(queryParams);

    }

    public static void SetBackLink(this Controller controller, bool isFileUploadJourneyInvokedViaRegistration, bool isResubmission, int? registrationYear = null, RegistrationJourney? registrationJourney = null)
    {
        var backLink = controller.Url.Content($"~/{(isFileUploadJourneyInvokedViaRegistration ? PagePaths.RegistrationTaskList : PagePaths.FileUploadCompanyDetailsSubLanding)}");
        controller.ViewBag.backLinkToDisplay = backLink.AppendBackLink(isResubmission, registrationYear, registrationJourney);
    }
}
