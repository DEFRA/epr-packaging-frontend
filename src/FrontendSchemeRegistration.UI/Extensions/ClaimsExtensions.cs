namespace FrontendSchemeRegistration.UI.Extensions;

using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Extensions;
using EPR.Common.Authorization.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

public static class ClaimsExtensions
{
    public static UserData GetUserData(this ClaimsPrincipal claimsPrincipal)
    {
        var userDataClaim = claimsPrincipal.Claims.First(c => c.Type == ClaimTypes.UserData);

        return JsonSerializer.Deserialize<UserData>(userDataClaim.Value);
    }

    public static Guid? GetOrganisationId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.GetUserData()?.Organisations?.FirstOrDefault()?.Id;
    }

    public static UserData? TryGetUserData(this ClaimsPrincipal claimsPrincipal)
    {
        try
        {
            return claimsPrincipal.GetUserData();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task UpdateUserDataClaimsAndSignInAsync(HttpContext httpContext, UserData userData)
    {
        var claimsIdentity = httpContext.User.Identity as ClaimsIdentity;
        var claim = claimsIdentity?.FindFirst(ClaimTypes.UserData);
        if (claim != null)
        {
            claimsIdentity?.RemoveClaim(claim);
        }

        var claims = new List<Claim> { new(ClaimTypes.UserData, JsonSerializer.Serialize(userData)) };
        claimsIdentity = new ClaimsIdentity(httpContext.User.Identity, claims);
        var principal = new ClaimsPrincipal(claimsIdentity);
        var properties = httpContext.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Properties;

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        // We need to set the user data in the http context here to ensure it is accessible in this request
        httpContext.User.AddOrUpdateUserData(userData);
    }
}