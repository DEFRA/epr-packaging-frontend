namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

[ExcludeFromCodeCoverage]
public class StubAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ICustomClaims customClaims,
    IHttpContextAccessor httpContextAccessor) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!httpContextAccessor.HttpContext.Request.Cookies.ContainsKey(StubAuthConstants.StubAuthCookieName))
        {
            return AuthenticateResult.Fail("");
        }
        
        var claims = new List<Claim>();

        var identity = new ClaimsIdentity(claims, "Epr-stub");
        var principal = new ClaimsPrincipal(identity);

        var additionalClaims = await customClaims.GetCustomClaims(new TokenValidatedContext(
            httpContextAccessor.HttpContext, Scheme, new OpenIdConnectOptions(), principal,
            new AuthenticationProperties()));
        claims.AddRange(additionalClaims);
        principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Epr-stub"));
        

        var ticket = new AuthenticationTicket(principal, "Epr-stub");
        
        var result = AuthenticateResult.Success(ticket);

        return result;
    }
}