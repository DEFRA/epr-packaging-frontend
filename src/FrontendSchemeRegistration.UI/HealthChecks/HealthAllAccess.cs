namespace FrontendSchemeRegistration.UI.HealthChecks;

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

public static class HealthAllAccess
{
    public static bool IsValid(HttpRequest request, HealthAllOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Token)
            || !request.Headers.TryGetValue(options.HeaderName, out var suppliedValues)
            || suppliedValues.Count != 1)
        {
            return false;
        }

        var supplied = Encoding.UTF8.GetBytes(suppliedValues[0]!);
        var expected = Encoding.UTF8.GetBytes(options.Token);

        return supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected);
    }
}
