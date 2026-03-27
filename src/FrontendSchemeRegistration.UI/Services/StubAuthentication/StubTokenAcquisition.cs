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
        return Task.FromResult(BuildStubToken());
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
        return Task.FromResult(BuildStubToken());
    }

    // Encodes both the UserId and Email into the bearer token so the mock
    // server can use email keywords to vary its responses.  Format: {userId}::{email}
    private string BuildStubToken()
    {
        var claims = httpContextAccessor.HttpContext.User.Claims;
        var userId = claims.GetClaim(ClaimTypes.NameIdentifier);
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return string.IsNullOrEmpty(email) ? userId : $"{userId}::{email}";
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