using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.ViewComponents
{
    public class ApprovedPerconInvitationSubmittedNotification : ViewComponent
    {
        private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

        public ApprovedPerconInvitationSubmittedNotification(ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
            var message = session.NominatedApprovedPersonSession.IsNominationSubmittedSuccessfully;
            session.NominatedApprovedPersonSession.IsNominationSubmittedSuccessfully = false;
            await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

            return View(model: message);
        }
    }
}
