using Microsoft.AspNetCore.WebUtilities;

namespace FrontendSchemeRegistration.UI.Extensions;

using Sessions;


public static class QueryStringExtensions
{
    public static RouteValueDictionary BuildRouteValues(Guid? submissionId = null, bool? isResubmission = null, int? registrationYear = null, ProducerSize? producerSize = null)
    {

        var routeValues = new RouteValueDictionary();

        if(submissionId.HasValue && submissionId.Value != Guid.Empty)
            routeValues["submissionId"] = submissionId.Value;

        if (isResubmission.HasValue && isResubmission.Value)
            routeValues["IsResubmission"] = true;

        if(registrationYear.HasValue && registrationYear.Value > 0)
            routeValues["registrationyear"] = registrationYear.Value;

        if(producerSize.HasValue)
            routeValues["producersize"] = producerSize.Value.ToString();
        
        return routeValues;
    }

    public static string AppendResubmissionFlagToQueryString(this string link, IDictionary<string, string> parameters)
    {
        if (parameters == null || !parameters.Any())
            return link;

        return QueryHelpers.AddQueryString(link, parameters);
    }
}
