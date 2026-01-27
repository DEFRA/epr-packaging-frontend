namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using TokenValidatedContext = Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext;

public interface IStubAuthenticationService
{
    void AddStubAuth(IResponseCookies cookies, StubAuthUserDetails model, bool isEssential = false);
    Task<ClaimsPrincipal> CreateClaimsPrincipal(StubAuthUserDetails model);
}

[ExcludeFromCodeCoverage]
public class StubAuthenticationService(IHttpContextAccessor httpContextAccessor, ICustomClaims customClaims) : IStubAuthenticationService
{
    public void AddStubAuth(IResponseCookies cookies, StubAuthUserDetails model, bool isEssential = false)
    {
        //This is for debug only
#if !DEBUG
        return;
#endif

        var authCookie = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddHours(1),
            Path = "/",
            Domain = "localhost",
            Secure = true,
            HttpOnly = true,
            IsEssential = isEssential,
            SameSite = SameSiteMode.None
        };
        cookies.Append(StubAuthConstants.StubAuthCookieName, JsonSerializer.Serialize(model), authCookie);
    }

    public async Task<ClaimsPrincipal> CreateClaimsPrincipal(StubAuthUserDetails model)
    {
        //This is for debug only
#if !DEBUG
        return new ClaimsPrincipal();
#endif
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, model.UserId.ToString()),
            new(ClaimTypes.Email, model.Email),
            new("sub", model.UserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
        var principal =  new ClaimsPrincipal(identity);

        if (customClaims != null)
        {
            var extraClaims = await customClaims.GetCustomClaims(
                new TokenValidatedContext(httpContextAccessor.HttpContext,
                    new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookie",typeof(StubAuthHandler)),
                    new OpenIdConnectOptions(),
                    principal,
                    new AuthenticationProperties()));
            
            claims.AddRange(extraClaims);
            principal = new ClaimsPrincipal(new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme));
        }
        
        return principal;
    }
}