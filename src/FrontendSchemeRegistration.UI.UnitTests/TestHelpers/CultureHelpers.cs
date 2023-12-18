namespace FrontendSchemeRegistration.UI.UnitTests.TestHelpers;

using System.Globalization;

public static class CultureHelpers
{
    public static void SetCulture(string culture)
    {
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
    }
}