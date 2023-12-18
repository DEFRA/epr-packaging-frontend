namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class SubmissionIdActionFilterAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _pagePath;

    public SubmissionIdActionFilterAttribute(string pagePath)
    {
        _pagePath = pagePath;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasQueryParameter = context.HttpContext.Request.Query.TryGetValue("submissionId", out var submissionIdString);

        if (!hasQueryParameter || !Guid.TryParse(submissionIdString, out _))
        {
            context.Result = new RedirectResult($"~{_pagePath}");
            return;
        }

        await next();
    }
}