﻿namespace FrontendSchemeRegistration.UI.Controllers;

using ControllerExtensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Controller used in web apps to manage accounts.
/// </summary>
[Route("[controller]/[action]")]
public class AccountController : Controller
{
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
        var callbackUrl = Url.Action(action: "SignedOut", controller: nameof(HomeController).RemoveControllerFromName(), values: null, protocol: Request.Scheme);

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
}