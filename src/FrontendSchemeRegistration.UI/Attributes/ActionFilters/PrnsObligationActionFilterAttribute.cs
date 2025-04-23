namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters;

using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
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

		if (!IsValidSession(session))
		{
			RedirectToRoot(context);
			return;
		}

		var userData = session.UserData;
		var organisations = userData?.Organisations;
		var organisation = organisations?.FirstOrDefault();

		if (organisation == null)
		{
			RedirectToRoot(context);
			return;
		}

		// If the organisation is a ComplianceScheme, check registration session and selected compliance scheme
		if (organisation.OrganisationRole == OrganisationRoles.ComplianceScheme && session.RegistrationSession?.SelectedComplianceScheme == null)
		{
			RedirectToRoot(context);
			return;
		}

		// Continue to the next action filter or action method
		await next();
	}

	private static bool IsValidSession(FrontendSchemeRegistrationSession session)
	{
		return session != null && session.UserData != null;
	}

	private static void RedirectToRoot(ActionExecutingContext context)
	{
		context.Result = new RedirectResult($"~{PagePaths.Root}");
	}
}