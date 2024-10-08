﻿namespace FrontendSchemeRegistration.Application.Extensions;

using System.Security.Claims;

public static class ClaimsExtensions
{
    public static string GetClaim(this IEnumerable<Claim> claims, string claimName)
    {
        var claimValue = claims?.ToList().Find(c => c.Type == claimName);

        return claimValue?.Value;
    }
}