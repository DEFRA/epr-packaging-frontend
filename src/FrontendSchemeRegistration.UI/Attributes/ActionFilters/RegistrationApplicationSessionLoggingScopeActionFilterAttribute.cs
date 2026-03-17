namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

/// <summary>
/// Adds a structured logging scope for the Registration Application session (if present).
/// Best-effort: never redirects/blocks execution.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RegistrationApplicationSessionLoggingScopeActionFilterAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<RegistrationApplicationSessionLoggingScopeActionFilterAttribute>>();

        var sessionManager = context.HttpContext.RequestServices
            .GetRequiredService<ISessionManager<RegistrationApplicationSession>>();

        var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

        if (session is null)
        {
            using (logger.BeginScope(new { HasRegistrationApplicationSession = false }))
            {
                await next();
            }

            return;
        }

        using (logger.BeginScope(new
               {
                   HasRegistrationApplicationSession = true,
                   session.SubmissionId,
                   session.ApplicationReferenceNumber
               }))
        {
            await next();
        }
    }
}

