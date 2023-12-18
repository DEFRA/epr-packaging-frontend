namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using Application.Constants;
using Constants;
using EPR.Common.Authorization.Sessions;
using Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sessions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ComplianceSchemeIdActionFilterAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userData = context.HttpContext.User.GetUserData();
        var organisation = userData.Organisations.First();

        if (organisation is { OrganisationRole: OrganisationRoles.ComplianceScheme })
        {
            var sessionManager = context.HttpContext.RequestServices.GetService<ISessionManager<FrontendSchemeRegistrationSession>>();
            var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

            if (session?.RegistrationSession.SelectedComplianceScheme?.Id is null)
            {
                context.Result = new RedirectResult($"~{PagePaths.ComplianceSchemeLanding}");
                return;
            }
        }

        await next();
    }
}