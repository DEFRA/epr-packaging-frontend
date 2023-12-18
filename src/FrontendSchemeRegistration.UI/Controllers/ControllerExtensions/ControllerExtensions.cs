namespace FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;

public static class ControllerExtensions
{
    public static string RemoveControllerFromName(this string controllerName)
    {
        return controllerName.Replace("Controller", string.Empty);
    }
}