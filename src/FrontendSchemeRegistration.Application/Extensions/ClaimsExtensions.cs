namespace FrontendSchemeRegistration.Application.Extensions;

using System.Security.Claims;

public static class ClaimsExtensions
{
    public static string GetClaim(this IEnumerable<Claim> claims, string claimName)
    {
        var claimsArray = claims as Claim[] ?? claims.ToArray();

        var claimValue = claimsArray.FirstOrDefault(c => c.Type == claimName);

        return claimValue?.Value;
    }
}