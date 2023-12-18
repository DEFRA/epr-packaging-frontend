namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sessions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SubmissionPeriodActionFilterAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _pagePath;

    public SubmissionPeriodActionFilterAttribute(string pagePath)
    {
        _pagePath = pagePath;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sessionManager = context.HttpContext.RequestServices.GetService<ISessionManager<FrontendSchemeRegistrationSession>>();
        var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

        if (session?.RegistrationSession.SubmissionPeriod is null)
        {
            context.Result = new RedirectResult($"~{_pagePath}");
            return;
        }

        await next();
    }
}