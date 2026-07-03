namespace FrontendSchemeRegistration.UI.Controllers;

using Application.Options;
using ControllerExtensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Controller used in web apps to manage accounts.
/// </summary>
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly CsocOptions _csocOptions;

    public AccountController(IOptions<CsocOptions> csocOptions)
    {
        _csocOptions = csocOptions.Value;
    }

    /// <summary>
    /// Handles user sign in.
    /// </summary>
    /// <param name="scheme">Authentication scheme.</param>
    /// <param name="redirectUri">Redirect URI.</param>
    /// <returns>Challenge generating a redirect to Azure AD to sign in the user.</returns>
    [HttpGet("{scheme?}")]
    [AllowAnonymous]
    public IActionResult SignIn(
        [FromRoute] string? scheme,
        [FromQuery] string redirectUri)
    {
        scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
        string redirect;
        if (!string.IsNullOrEmpty(redirectUri) && Url.IsLocalUrl(redirectUri))
        {
            redirect = redirectUri;
        }
        else
        {
            redirect = Url.Content("~/");
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirect },
            scheme);
    }

    /// <summary>
    /// Clears the local Azure session cookies without triggering Azure AD B2C logout.
    /// Used when signing out from a linked CDP child app.
    /// </summary>
    /// <returns>Redirect to sign in.</returns>
    [ExcludeFromCodeCoverage(Justification = "Unable to mock authentication")]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ClearSession()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect(Url.Action(nameof(SignIn), nameof(AccountController).RemoveControllerFromName())!);
    }

    /// <summary>
    /// Handles the user sign-out.
    /// </summary>
    /// <param name="scheme">Authentication scheme.</param>
    /// <returns>Sign out result.</returns>
    [ExcludeFromCodeCoverage(Justification = "Unable to mock authentication")]
    [HttpGet("{scheme?}")]
    public IActionResult SignOut(
        [FromRoute] string? scheme)
    {
        if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
        {
            if (AppServicesAuthenticationInformation.LogoutUrl != null)
            {
                return LocalRedirect(AppServicesAuthenticationInformation.LogoutUrl);
            }

            return Ok();
        }

        scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
        var callbackUrl = GetWasteObligationsClearSessionUrl()
            ?? Url.Action(
                action: "SignedOut",
                controller: nameof(HomeController).RemoveControllerFromName(),
                values: null,
                protocol: Request.Scheme);

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = callbackUrl,
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            scheme);
    }

    /// <summary>
    /// Handles the session timeout sign-out.
    /// </summary>
    /// <param name="scheme">Authentication scheme.</param>
    /// <returns>Session Timeout Sign out result.</returns>
    [ExcludeFromCodeCoverage(Justification = "Unable to mock authentication")]
    [HttpGet("{scheme?}")]
    [AllowAnonymous]
    public IActionResult SessionSignOut([FromRoute] string? scheme)
    {
        if (AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled)
        {
            if (AppServicesAuthenticationInformation.LogoutUrl != null)
            {
                return LocalRedirect(AppServicesAuthenticationInformation.LogoutUrl);
            }

            return Ok();
        }

        scheme ??= OpenIdConnectDefaults.AuthenticationScheme;
        var callbackUrl = Url.Action(action: "TimeoutSignedOut", controller: nameof(HomeController).RemoveControllerFromName(), values: null, protocol: Request.Scheme);

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = callbackUrl,
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            scheme);
    }

    [ExcludeFromCodeCoverage(Justification = "Unable to mock authentication")]
    [HttpGet]
    [AllowAnonymous]
    public IActionResult KeepSessionAlive()
    {
        // Refresh session by interacting with it
        HttpContext.Session.SetString("LastPing", DateTime.UtcNow.ToString());
        return Ok(new { message = "Session extended" });
    }

    private string? GetWasteObligationsClearSessionUrl()
    {
        var baseAddress = _csocOptions.WasteObligationsBaseAddress?.TrimEnd('/');
        return string.IsNullOrEmpty(baseAddress) ? null : $"{baseAddress}/clear-session";
    }
}
