namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Application.Extensions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

[ExcludeFromCodeCoverage]
public class StubTokenAcquisition(IHttpContextAccessor httpContextAccessor) : ITokenAcquisition
{
    public Task<string> GetAccessTokenForUserAsync(
        IEnumerable<string> scopes,
        string? authenticationScheme,
        string? tenantId = null,
        string? userFlow = null,
        ClaimsPrincipal? user = null,
        TokenAcquisitionOptions? tokenAcquisitionOptions = null)
    {
        //This gets the id of the user from the claims and set as the
        // bearer token for the stub - this is then pulled out in the mock server to build requests
        var userId = httpContextAccessor.HttpContext.User.Claims.GetClaim(ClaimTypes.NameIdentifier);
        return Task.FromResult(userId);
    }

    public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
        IEnumerable<string> scopes,
        string? authenticationScheme,
        string? tenantId = null,
        string? userFlow = null,
        ClaimsPrincipal? user = null,
        TokenAcquisitionOptions? tokenAcquisitionOptions = null)
    {
        throw new NotImplementedException("StubTokenAcquisition does not provide AuthenticationResult in stub mode.");
    }

    public Task<string> GetAccessTokenForAppAsync(
        string scope,
        string? authenticationScheme,
        string? tenant = null,
        TokenAcquisitionOptions? tokenAcquisitionOptions = null)
    {
        var userId = httpContextAccessor.HttpContext.User.Claims.GetClaim(ClaimTypes.NameIdentifier);
        return Task.FromResult(userId);
    }

    public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
        string scope,
        string? authenticationScheme,
        string? tenant = null,
        TokenAcquisitionOptions? tokenAcquisitionOptions = null)
    {
        throw new NotImplementedException("StubTokenAcquisition does not provide AuthenticationResult in stub mode.");
    }

    public void ReplyForbiddenWithWwwAuthenticateHeader(
        IEnumerable<string> scopes,
        MsalUiRequiredException msalServiceException,
        string? authenticationScheme,
        HttpResponse? httpResponse = null)
    {
    }

    public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
    {
        return authenticationScheme ?? OpenIdConnectDefaults.AuthenticationScheme;
    }

    public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
        IEnumerable<string> scopes,
        MsalUiRequiredException msalServiceException,
        HttpResponse? httpResponse = null)
    {
        return Task.CompletedTask;
    }
}