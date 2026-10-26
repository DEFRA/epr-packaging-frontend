namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.FeatureManagement;

/// <summary>
///     Applies a year passed on the query string of a GET request as the selected obligation year. 2025 and
///     earlier redirect to the compliance certificate page, as they do on the manage obligations pages.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequestedObligationYearAttribute() : TypeFilterAttribute(typeof(RequestedObligationYearFilter));

public class RequestedObligationYearFilter(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IFeatureManager featureManager,
    TimeProvider timeProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!HttpMethods.IsGet(context.HttpContext.Request.Method)
            || !int.TryParse(context.HttpContext.Request.Query["year"], out var requestedYear)
            || !await featureManager.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations))
        {
            await next();
            return;
        }

        var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

        if (session is null)
        {
            await next();
            return;
        }

        if (requestedYear <= ObligationYearOptions.CsocUnavailablePastYear)
        {
            session.PrnSession.SelectedObligationYear = ObligationYearOptions.CsocUnavailablePastYear;
            await sessionManager.SaveSessionAsync(context.HttpContext.Session, session);

            context.Result = new RedirectToActionResult(nameof(PrnsObligationController.ComplianceCertificate), nameof(PrnsObligationController).RemoveControllerFromName(), null);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var selectableYears = ObligationYearOptions.GetSelectableYears(now.GetComplianceYear(), now.IsInDecemberJanuaryFlashWindow());

        if (selectableYears.Contains(requestedYear))
        {
            session.PrnSession.SelectedObligationYear = requestedYear;
            await sessionManager.SaveSessionAsync(context.HttpContext.Session, session);
        }

        await next();
    }
}
