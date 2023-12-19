using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.ViewComponents;

public class PendingApprovalNotificationViewComponent : ViewComponent
{
    private readonly ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    public PendingApprovalNotificationViewComponent(ISessionManager<FrontendSchemeRegistrationSession> sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var session = await _sessionManager.GetSessionAsync(HttpContext.Session) ?? new FrontendSchemeRegistrationSession();
        var message = session.RegistrationSession.NotificationMessage;
        session.RegistrationSession.NotificationMessage = null;
        await _sessionManager.SaveSessionAsync(HttpContext.Session, session);

        return View(model: message);
    }
}