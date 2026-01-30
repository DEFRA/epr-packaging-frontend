namespace FrontendSchemeRegistration.UI.ComponentTests.Extensions;

using System.Diagnostics.CodeAnalysis;
using Data;
using Reqnroll;

[ExcludeFromCodeCoverage]
public static class ScenarioContextExtensions
{
    public static Pages.Page GetPage(this ScenarioContext context, string pageName)
    {
        var page = Pages.GetPages()
            .SingleOrDefault(x => x.Name.Equals(pageName, StringComparison.CurrentCultureIgnoreCase));

        if (page == null)
        {
            throw new InvalidOperationException($"Unable to get page {pageName}");
        }
        return page;
    }

    public static Responses.Response GetStatusCode(this ScenarioContext context, string responseName)
    {
        var response = Responses.GetResponses()
            .SingleOrDefault(x => x.Name.Equals(responseName, StringComparison.CurrentCultureIgnoreCase));

        if (response == null)
        {
            throw new InvalidOperationException($"Unable to get response {responseName}");
        }
        return response;
    }
}