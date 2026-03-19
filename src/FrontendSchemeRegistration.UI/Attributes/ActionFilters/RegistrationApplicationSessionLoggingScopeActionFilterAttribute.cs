namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

/// <summary>
/// Adds a structured logging scope for the Registration Application session (if present).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RegistrationApplicationSessionLoggingScopeActionFilterAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<RegistrationApplicationSessionLoggingScopeActionFilterAttribute>>();
        
        var registrationApplicationSessionManager = context.HttpContext.RequestServices
            .GetRequiredService<ISessionManager<RegistrationApplicationSession>>();

        var frontendSchemeRegistrationSessionManager = context.HttpContext.RequestServices
            .GetRequiredService<ISessionManager<FrontendSchemeRegistrationSession>>();

        var actionName = context.ActionDescriptor.DisplayName ?? "UnknownAction";
        var registrationApplicationSession = await registrationApplicationSessionManager.GetSessionAsync(context.HttpContext.Session);
        var frontendSchemeRegistrationSession = await frontendSchemeRegistrationSessionManager.GetSessionAsync(context.HttpContext.Session);

        using (logger.BeginScope("RegistrationApplication action invoked: {Action}", actionName))
        {
            if (registrationApplicationSession is null)
            {
                logger.LogInformation("OnActionEntry: {Action}: RegistrationSession is null", actionName);
                await next();

                return;
            }

            using (logger.AddScopedData(new Dictionary<string, object>
            {
                ["RegistrationApplicationSession.SubmissionId"] = registrationApplicationSession.SubmissionId,
                ["RegistrationApplicationSession.RegistrationJourney"] = registrationApplicationSession.RegistrationJourney,
                ["RegistrationApplicationSession.SubmissionPeriod"] = registrationApplicationSession.SubmissionPeriod,
                ["RegistrationApplicationSession.ApplicationReferenceNumber"] = registrationApplicationSession.ApplicationReferenceNumber,
                ["FrontendSchemeRegistrationSession.ApplicationReferenceNumber"] = frontendSchemeRegistrationSession?.RegistrationSession?.ApplicationReferenceNumber,
                ["FrontendSchemeRegistrationSession.IsResubmission"] = frontendSchemeRegistrationSession?.RegistrationSession?.IsResubmission
            }))
            {
                logger.LogInformation("OnActionEntry: RegistrationSession found");
                await next();
            }
        }
    }
}
