namespace FrontendSchemeRegistration.UI.Controllers;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.StubAuthentication;

[Route("")]
public class ServicesController(IStubAuthenticationService stubAuthenticationService, IWebHostEnvironment webHostEnvironment) : Controller
{
    [HttpGet]
    [Route("services/account-details", Name= StubAuthRouteNames.StubAccountGet)]
    [AllowAnonymous]
    public IActionResult AccountDetails([FromQuery] string returnUrl)
    {
        if (!webHostEnvironment.EnvironmentName.Equals("ComponentTest", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return View("AccountDetails", new StubAuthenticationViewModel
        {
            ReturnUrl = returnUrl
        });
    }
    
    [HttpPost]
    [Route("services/account-details", Name = StubAuthRouteNames.StubAccountPost)]
    [AllowAnonymous]
    public async Task<IActionResult> AccountDetails(StubAuthenticationViewModel model)
    {
        if (!webHostEnvironment.EnvironmentName.Equals("ComponentTest", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
        var claims = await stubAuthenticationService.CreateClaimsPrincipal(model);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claims, new AuthenticationProperties());
        
        return RedirectToRoute(StubAuthRouteNames.SignedIn, new { returnUrl = model.ReturnUrl });
    }

    [HttpGet]
    [Authorize]
    [Route("services/stub-auth", Name = StubAuthRouteNames.SignedIn)]
    public IActionResult StubSignedIn([FromQuery] string returnUrl)
    {
        if (!webHostEnvironment.EnvironmentName.Equals("ComponentTest", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
        var viewModel = new SignedInAccountViewModel
        {
            ReturnUrl = returnUrl,
            Email = User.Claims.FirstOrDefault(c=>c.Type.Equals(ClaimTypes.Email))?.Value,
            Id = User.Claims.FirstOrDefault(c=>c.Type.Equals(ClaimTypes.NameIdentifier))?.Value
        };
        
        return View(viewModel);
    }
}

public class SignedInAccountViewModel
{
    public string Email { get; set; }
    public string Id { get; set; }
    public string ReturnUrl { get; set; }
}

public class StubAuthenticationViewModel : StubAuthUserDetails
{
    public string ReturnUrl { get; set; }
}

public static class StubAuthRouteNames
{
    public const string SignedIn = "signedin";
    public const string StubAccountPost = "stubaccountpost";
    public const string StubAccountGet = "stubaccountget";
}