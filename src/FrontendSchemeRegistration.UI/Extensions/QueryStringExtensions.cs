﻿using Microsoft.AspNetCore.WebUtilities;

namespace FrontendSchemeRegistration.UI.Extensions;
public static class QueryStringExtensions
{
    public static RouteValueDictionary BuildRouteValues(Guid? submissionId = null, bool? isResubmission = null, int? registrationYear = null)
    {

        var routeValues = new RouteValueDictionary();

        if(submissionId.HasValue && submissionId.Value != Guid.Empty)
            routeValues["submissionId"] = submissionId.Value;

        if (isResubmission.HasValue && isResubmission.Value)
            routeValues["IsResubmission"] = true;

        if(registrationYear.HasValue && registrationYear.Value > 0)
            routeValues["registrationyear"] = registrationYear.Value;

        return routeValues;
    }

    public static string AppendResubmissionFlagToQueryString(this string link, IDictionary<string, string> parameters)
    {
        if (parameters == null || !parameters.Any())
            return link;

        return QueryHelpers.AddQueryString(link, parameters);
    }
}
