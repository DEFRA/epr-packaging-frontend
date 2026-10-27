namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.FeatureManagement;

/// <summary>
///     Redirects to the compliance certificate page when the obligation year selected in the session is 2025 or
///     earlier, as there are no obligations to show for those years. Runs after <see cref="RequestedObligationYearAttribute"/>
///     so that a year on the query string is applied before the session is checked.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RedirectPastObligationYearAttribute : TypeFilterAttribute
{
    public RedirectPastObligationYearAttribute() : base(typeof(RedirectPastObligationYearFilter))
    {
        // Runs after RequestedObligationYearAttribute, which keeps the default order
        Order = 1;
    }
}

public class RedirectPastObligationYearFilter(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IFeatureManager featureManager) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await featureManager.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations))
        {
            var session = await sessionManager.GetSessionAsync(context.HttpContext.Session);

            if (session?.PrnSession.SelectedObligationYear <= ObligationYearOptions.CsocUnavailablePastYear)
            {
                context.Result = new RedirectToActionResult(
                    nameof(PrnsObligationController.ComplianceCertificate),
                    nameof(PrnsObligationController).RemoveControllerFromName(),
                    null);
                return;
            }
        }

        await next();
    }
}
