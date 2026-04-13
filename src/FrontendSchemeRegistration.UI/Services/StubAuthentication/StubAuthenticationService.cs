namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using TokenValidatedContext = Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext;

public interface IStubAuthenticationService
{
    void AddStubAuth(IResponseCookies cookies, StubAuthUserDetails model, bool isEssential = false);
    Task<ClaimsPrincipal> CreateClaimsPrincipal(StubAuthUserDetails model);
}

[ExcludeFromCodeCoverage]
public class StubAuthenticationService(IHttpContextAccessor httpContextAccessor, ICustomClaims customClaims, IWebHostEnvironment webHostEnvironment) : IStubAuthenticationService
{
    public void AddStubAuth(IResponseCookies cookies, StubAuthUserDetails model, bool isEssential = false)
    {
        if (!webHostEnvironment.EnvironmentName.Equals("ComponentTest", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

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
        if (!webHostEnvironment.EnvironmentName.Equals("ComponentTest", StringComparison.OrdinalIgnoreCase))
        {
            return new ClaimsPrincipal();
        }

        var userId = string.IsNullOrWhiteSpace(model.UserId)
            ? GenerateStableId(model.Email)
            : model.UserId;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, model.Email),
            new("sub", userId)
        };
        var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
        var principal =  new ClaimsPrincipal(identity);

        // Set the new principal on HttpContext so that StubTokenAcquisition reads the
        // new UserId (as the bearer token) when fetching user data from the API.
        httpContextAccessor.HttpContext.User = principal;

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

    private static string GenerateStableId(string email)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return new Guid(hash.AsSpan(0, 16)).ToString();
    }
}