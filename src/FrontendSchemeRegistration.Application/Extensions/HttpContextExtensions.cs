namespace FrontendSchemeRegistration.Application.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Extensions;
using EPR.Common.Authorization.Models;
using Microsoft.AspNetCore.Http;

[ExcludeFromCodeCoverage]
public static class HttpContextExtensions
{
    public const string DefraIdentityName = "Defra.Identity";
    public const string TokenName = "Defra.Identity.Token";

    public static void AddAuthCookie(this HttpContext context, string value) =>
        context.Response.Cookies.Append("Authorization", "Bearer " + value, GetCookieOptions());

    public static void SetToken(this HttpContext context, string? token)
    {
        context.Items.TryAdd(TokenName, token);
    }

    public static string GetToken(this HttpContext context)
    {
        if (!context.Items.TryGetValue(TokenName, out var value))
        {
            throw new InvalidOperationException("Context does not have a Defra Identity Token");
        }

        return value?.ToString();
    }

    public static void SetClaims(this HttpContext context, IEnumerable<Claim> claims)
    {
        context.Items.TryAdd(DefraIdentityName, claims);
    }

    public static UserData GetUserData(this HttpContext context)
    {
        string? claimsString = context.User.Claims.GetClaim(ClaimTypes.UserData);
        return claimsString is null ? new UserData() : JsonSerializer.Deserialize<UserData>(claimsString);
    }

    public static string GetName(this HttpContext context)
    {
        var userData = GetUserData(context);
        return $"{userData.FirstName} {userData.LastName}";
    }

    public static Guid GetCustomerId(this HttpContext context)
    {
        return context.User.UserId();
    }

    public static string Email(this HttpContext context) => GetUserData(context).Email;

    public static Guid GetCustomerOrganisationId(this HttpContext context)
    {
        return GetUserData(context).Organisations.FirstOrDefault()!.Id.Value;
    }

    private static CookieOptions GetCookieOptions() => new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = true,
    };
}