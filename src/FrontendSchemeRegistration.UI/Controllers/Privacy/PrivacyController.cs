namespace FrontendSchemeRegistration.UI.Controllers.Privacy;

using Application.Constants;
using Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ViewModels.Privacy;

[AllowAnonymous]
public class PrivacyController : Controller
{
    private readonly ExternalUrlOptions _urlOptions;
    private readonly EmailAddressOptions _emailOptions;
    private readonly SiteDateOptions _siteDateOptions;

    public PrivacyController(
        IOptions<ExternalUrlOptions> urlOptions,
        IOptions<EmailAddressOptions> emailOptions,
        IOptions<SiteDateOptions> siteDateOptions)
    {
        _urlOptions = urlOptions.Value;
        _emailOptions = emailOptions.Value;
        _siteDateOptions = siteDateOptions.Value;
    }

    [HttpGet]
    [Route(PagePaths.Privacy)]
    public IActionResult Detail(string returnUrl)
    {
        var allowedBackValues = new[] { "/report-data", "/create-account", "/manage-account" };
        var validBackLink = !string.IsNullOrEmpty(returnUrl) && allowedBackValues.Any(a => returnUrl.StartsWith(a));
        string redirect;
        if (Url.IsLocalUrl(returnUrl) && validBackLink)
        {
            redirect = returnUrl;
        }
        else
        {
            redirect = Url.Content("~/");
        }

        var model = new PrivacyViewModel
        {
            DataProtectionPublicRegisterUrl = _urlOptions.PrivacyDataProtectionPublicRegister,
            DefrasPersonalInformationCharterUrl = _urlOptions.PrivacyDefrasPersonalInformationCharter,
            InformationCommissionerUrl = _urlOptions.PrivacyInformationCommissioner,
            ScottishEnvironmentalProtectionAgencyUrl = _urlOptions.PrivacyScottishEnvironmentalProtectionAgency,
            NationalResourcesWalesUrl = _urlOptions.PrivacyNationalResourcesWales,
            NorthernIrelandEnvironmentAgencyUrl = _urlOptions.PrivacyNorthernIrelandEnvironmentAgency,
            EnvironmentAgencyUrl = _urlOptions.PrivacyEnvironmentAgency,
            DataProtectionEmail = _emailOptions.DataProtection,
            DefraGroupProtectionOfficerEmail = _emailOptions.DefraGroupProtectionOfficer,
            LastUpdated = _siteDateOptions.PrivacyLastUpdated.ToString(_siteDateOptions.DateFormat)
        };
        ViewBag.BackLinkToDisplay = redirect;
        ViewBag.CurrentPage = redirect;

        return View(model);
    }
}
