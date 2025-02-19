namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sessions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PrnsObligationActionFilterAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sessionManager = context.HttpContext.RequestServices.GetService<ISessionManager<FrontendSchemeRegistrationSession>>();
        var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

        if (session is null)
        {
            context.Result = new RedirectResult($"~{PagePaths.Root}");
            return;
        }

        await next();
    }
}