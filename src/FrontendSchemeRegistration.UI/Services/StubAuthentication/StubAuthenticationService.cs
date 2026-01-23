namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Application.Extensions;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using TokenValidatedContext = Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext;

public interface IStubAuthenticationService
{
    void AddStubAuth(IResponseCookies cookies, StubAuthUserDetails model, bool isEssential = false);
    Task<ClaimsPrincipal> CreateClaimsPrincipal(StubAuthUserDetails model);
}

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
        return;
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

public class StubAuthUserDetails
{
    public string Email { get; set; }
    public string UserId { get; set; }
}

public interface ICustomClaims
{
    Task<IEnumerable<Claim>> GetCustomClaims(TokenValidatedContext context);
}

public class CustomClaims(IUserAccountService userAccountService) : ICustomClaims
{
    public async Task<IEnumerable<Claim>> GetCustomClaims(TokenValidatedContext context)
    {
        //use this to get any extra claim data
        var userAccount = await userAccountService.GetUserAccount();


        var userData = new UserData
        {
            ServiceRoleId = userAccount.User.ServiceRoleId,
            ServiceRole = userAccount.User.ServiceRole,
            Service = userAccount.User.Service,
            FirstName = userAccount.User.FirstName,
            LastName = userAccount.User.LastName,
            Email = userAccount.User.Email,
            Id = userAccount.User.Id,
            EnrolmentStatus = userAccount.User.EnrolmentStatus,
            JobTitle = "Director",
            RoleInOrganisation = userAccount.User.RoleInOrganisation,
            Organisations = userAccount.User.Organisations.Select(x =>
                new Organisation
                {
                    Id = x.Id,
                    Name = x.OrganisationName,
                    OrganisationRole = x.OrganisationRole,
                    OrganisationType = x.OrganisationType,
                    OrganisationNumber = x.OrganisationNumber
                }).ToList()
        };
        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData)),
            new (ClaimConstants.ObjectId, userAccount.User.Id.ToString())
        };
    }
}

public class StubAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider timeProvider,
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

        if (customClaims != null)
        {
            var additionalClaims = await customClaims.GetCustomClaims(new TokenValidatedContext(
                httpContextAccessor.HttpContext, Scheme, new OpenIdConnectOptions(), principal,
                new AuthenticationProperties()));
            claims.AddRange(additionalClaims);
            principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Epr-stub"));
        }

        var ticket = new AuthenticationTicket(principal, "Epr-stub");
        
        var result = AuthenticateResult.Success(ticket);

        return result;
    }
}


public static class StubAuthConstants
{
    public const string StubAuthCookieName = ".epr_auth_stub";
}


//TODO - tidy this

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
        var userId = httpContextAccessor.HttpContext.User.Claims.GetClaim(ClaimTypes.NameIdentifier);
        //TODO this will get the id of the user from the claims and set as the
        // bearer token for the stub
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
        return Task.FromResult("stub-app-access-token");
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