namespace FrontendSchemeRegistration.UI.Middleware;

using Application.Constants;
using Application.Options;
using Constants;
using Controllers.Attributes;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Sessions;

public class JourneyAccessCheckerMiddleware
{
    private readonly RequestDelegate _next;

    public JourneyAccessCheckerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, ISessionManager<FrontendSchemeRegistrationSession> sessionManager, IOptions<GlobalVariables> globalVariables)
    {
        var endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;
        var attribute = endpoint?.Metadata.GetMetadata<JourneyAccessAttribute>();

        if (attribute == null)
        {
            await _next(httpContext);
            return;
        }

        string? pageToRedirect = null;
        var sessionValue = await sessionManager.GetSessionAsync(httpContext.Session);

        if (attribute.JourneyType is JourneyName.SchemeMembershipStart or JourneyName.SchemeMembership)
        {
            await HandleSchemeMembershipStart(httpContext, globalVariables, attribute, sessionValue);
            return;
        }

        pageToRedirect = attribute.JourneyType is JourneyName.NominatedDelegatedPersonStart or JourneyName.NominatedDelegatedPerson
            ? GetNominatedDelegatedPersonPageToRedirect(httpContext, globalVariables, pageToRedirect, attribute, sessionValue)
            : GetHomePageSelfManagedPageToRedirect(sessionValue, pageToRedirect, attribute);

        if (!string.IsNullOrEmpty(pageToRedirect))
        {
            httpContext.Response.Redirect(pageToRedirect);
            return;
        }

        await _next(httpContext);
    }

    private static string? GetHomePageSelfManagedPageToRedirect(FrontendSchemeRegistrationSession? sessionValue, string? pageToRedirect, JourneyAccessAttribute attribute)
    {
        if (sessionValue?.NominatedDelegatedPersonSession is null || sessionValue.RegistrationSession.Journey.Count == 0)
        {
            pageToRedirect = PagePaths.HomePageSelfManaged;
        }
        else if (!sessionValue.RegistrationSession.Journey.Contains(attribute.PagePath))
        {
            var count = sessionValue.RegistrationSession.Journey.Count;
            pageToRedirect = sessionValue.RegistrationSession.Journey[count - 1];
        }

        return pageToRedirect;
    }

    private static string? GetNominatedDelegatedPersonPageToRedirect(HttpContext httpContext, IOptions<GlobalVariables> globalVariables, string? pageToRedirect, JourneyAccessAttribute attribute, FrontendSchemeRegistrationSession? sessionValue)
    {
        var id = ParseRouteId(httpContext);

        if (id == null)
        {
            pageToRedirect = $"{globalVariables.Value.BasePath}/{PagePaths.HomePageSelfManaged}";
        }
        else if (attribute.JourneyType == JourneyName.NominatedDelegatedPerson)
        {
            if (sessionValue?.NominatedDelegatedPersonSession is null || sessionValue.NominatedDelegatedPersonSession.Journey.Count == 0)
            {
                pageToRedirect = $"{globalVariables.Value.BasePath}/{PagePaths.HomePageSelfManaged}";
            }
            else if (!sessionValue.NominatedDelegatedPersonSession.Journey.Contains($"{attribute.PagePath}/{id}"))
            {
                var count = sessionValue.NominatedDelegatedPersonSession.Journey.Count;
                pageToRedirect = $"{globalVariables.Value.BasePath}{sessionValue.NominatedDelegatedPersonSession.Journey[count - 1]}";
            }
        }

        return pageToRedirect;
    }

    private async Task HandleSchemeMembershipStart(HttpContext httpContext, IOptions<GlobalVariables> globalVariables, JourneyAccessAttribute attribute, FrontendSchemeRegistrationSession? sessionValue)
    {
        if (attribute.JourneyType == JourneyName.SchemeMembership &&
            sessionValue?.SchemeMembershipSession is not null &&
            sessionValue.SchemeMembershipSession.Journey.Count > 0 &&
            !sessionValue.SchemeMembershipSession.Journey.Contains(httpContext.Request.Path))
        {
            var count = sessionValue.SchemeMembershipSession.Journey.Count;
            var pageToRedirect = sessionValue.SchemeMembershipSession.Journey[count - 1];
            httpContext.Response.Redirect($"{globalVariables.Value.BasePath}{pageToRedirect}");
        }

        await _next(httpContext);
    }

    private static Guid? ParseRouteId(HttpContext context)
    {
        var id = context.GetRouteData().Values["id"];

        if (!Guid.TryParse(id?.ToString(), out var identifier))
        {
            return null;
        }

        return identifier;
    }
}
