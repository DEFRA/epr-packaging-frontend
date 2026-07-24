namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using Application.Constants;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Sessions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PomResubmissionSessionGuardActionFilterAttribute : Attribute, IAsyncActionFilter
{
    public bool RedirectOnMissingState { get; set; } = true;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.HttpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";

        if (!RedirectOnMissingState)
        {
            await next();
            return;
        }

        var sessionManager = context.HttpContext.RequestServices.GetService<ISessionManager<FrontendSchemeRegistrationSession>>();
        var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

        if (session?.PomResubmissionSession?.PackagingResubmissionApplicationSession?.SubmissionId is null)
        {
            context.Result = new RedirectResult($"~/{PagePaths.ResubmissionTaskList}");
            return;
        }

        await next();
    }
}
