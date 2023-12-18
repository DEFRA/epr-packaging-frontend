namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

public static class StringHelpers
{
    public static string EnsureLeadingSlash(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "/";
        }

        return input.StartsWith("/") ? input : "/" + input;
    }
}
