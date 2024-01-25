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

        if (attribute != null)
        {
            string? pageToRedirect = null;
            var sessionValue = await sessionManager.GetSessionAsync(httpContext.Session);

            if (attribute.JourneyType is JourneyName.SchemeMembershipStart or JourneyName.SchemeMembership)
            {
                if (attribute.JourneyType == JourneyName.SchemeMembership &&
                    sessionValue?.SchemeMembershipSession is not null &&
                    sessionValue.SchemeMembershipSession.Journey.Any() &&
                    !sessionValue.SchemeMembershipSession.Journey.Contains(httpContext.Request.Path))
                {
                    pageToRedirect = sessionValue.SchemeMembershipSession.Journey.Last();
                    httpContext.Response.Redirect($"{globalVariables.Value.BasePath}{pageToRedirect}");
                }

                await _next(httpContext);

                return;
            }

            if (attribute.JourneyType == JourneyName.NominatedDelegatedPersonStart ||
                attribute.JourneyType == JourneyName.NominatedDelegatedPerson)
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
                        pageToRedirect = $"{globalVariables.Value.BasePath}{sessionValue.NominatedDelegatedPersonSession.Journey.Last()}";
                    }
                }
            }
            else
            {
                if (sessionValue?.NominatedDelegatedPersonSession is null || sessionValue.RegistrationSession.Journey.Count == 0)
                {
                    pageToRedirect = PagePaths.HomePageSelfManaged;
                }
                else if (!sessionValue.RegistrationSession.Journey.Contains(attribute.PagePath))
                {
                    pageToRedirect = sessionValue.RegistrationSession.Journey.Last();
                }
            }

            if (!string.IsNullOrEmpty(pageToRedirect))
            {
                httpContext.Response.Redirect(pageToRedirect);
                return;
            }
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
