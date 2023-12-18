namespace FrontendSchemeRegistration.UI.Controllers.Attributes;

using Constants;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class JourneyAccessAttribute : Attribute
{
    public JourneyAccessAttribute(string pagePath, JourneyName journeyType = JourneyName.Registration)
    {
        PagePath = pagePath;
        JourneyType = journeyType;
    }

    public string PagePath { get; }

    public JourneyName JourneyType { get; }
}
