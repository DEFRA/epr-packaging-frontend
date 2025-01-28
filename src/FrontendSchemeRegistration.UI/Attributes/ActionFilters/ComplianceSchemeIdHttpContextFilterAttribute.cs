namespace FrontendSchemeRegistration.UI.Attributes.ActionFilters
{
    using EPR.Common.Authorization.Sessions;
    using Extensions;
    using Constants;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Sessions;
    using System.Threading.Tasks;
    using Application.Constants;

    public class ComplianceSchemeIdHttpContextFilterAttribute : IAsyncActionFilter
    {
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;
        public ComplianceSchemeIdHttpContextFilterAttribute(ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.Session == null)
            {
                // Exit early if the session is null
                await next();
                return;
            }
            var userData = context.HttpContext.User.GetUserData();
            var organisation = userData.Organisations.FirstOrDefault(o => o.OrganisationRole == OrganisationRoles.ComplianceScheme);
            if (organisation != null)
            {
                var session = await _sessionManager.GetSessionAsync(context.HttpContext.Session);
                if (session?.RegistrationSession?.SelectedComplianceScheme != null)
                {
                    context.HttpContext.Items[ComplianceScheme.ComplianceSchemeId] = session.RegistrationSession.SelectedComplianceScheme.Id;
                }
            }
            await next();
        }

    }
}
