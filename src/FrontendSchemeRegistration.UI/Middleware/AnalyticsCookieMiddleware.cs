namespace FrontendSchemeRegistration.UI.Middleware;

using Application.Options;
using Application.Services.Interfaces;
using Constants;
using Microsoft.Extensions.Options;

public class AnalyticsCookieMiddleware
{
    private readonly RequestDelegate _next;

    public AnalyticsCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        ICookieService cookieService,
        IOptions<GoogleAnalyticsOptions> googleAnalyticsOptions)
    {
        httpContext.Items[ContextKeys.UseGoogleAnalyticsCookieKey] = cookieService.HasUserAcceptedCookies(httpContext.Request.Cookies);
        httpContext.Items[ContextKeys.TagManagerContainerIdKey] = googleAnalyticsOptions.Value.TagManagerContainerId;

        await _next(httpContext);
    }
}