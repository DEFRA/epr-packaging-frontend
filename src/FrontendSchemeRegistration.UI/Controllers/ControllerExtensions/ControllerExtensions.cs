using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;

public static class ControllerExtensions
{
    public static string RemoveControllerFromName(this string controllerName)
    {
        return controllerName.Replace("Controller", string.Empty);
    }

    public static void EnsureApplicationReferenceIsPresent(this FrontendSchemeRegistrationSession? session)
    {
        if (!string.IsNullOrWhiteSpace(session.RegistrationSession.SubmissionPeriod)
            && session.RegistrationSession.SubmissionPeriod.Contains("January to December")
            && string.IsNullOrWhiteSpace(session.RegistrationSession.ApplicationReferenceNumber))
        {
            throw new InvalidOperationException("ApplicationReferenceNumber is required for Registration File Submission");
        }
    }
}