namespace FrontendSchemeRegistration.UI.ViewComponents;

using Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using ViewModels.Shared;

public class LanguageSwitcherViewComponent : ViewComponent
{
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;
    private readonly IFeatureManager _featureManager;

    public LanguageSwitcherViewComponent(IOptions<RequestLocalizationOptions> localizationOptions, IFeatureManager featureManager)
    {
        _localizationOptions = localizationOptions;
        _featureManager = featureManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
        var languageSwitcherModel = new LanguageSwitcherModel
        {
            SupportedCultures = _localizationOptions.Value.SupportedCultures!.ToList(),
            CurrentCulture = cultureFeature!.RequestCulture.Culture,
            ReturnUrl = GetReturnUrl(),
            ShowLanguageSwitcher = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.ShowLanguageSwitcher))
        };

        return View(languageSwitcherModel);
    }

    private string GetReturnUrl()
    {
        // UseStatusCodePagesWithReExecute rewrites Request.Path to the error handler before re-running the
        // pipeline, so on an error page the raw request would send the user back to /error itself. That loses
        // the status code the page was chosen from, turning a switch of language on Page not found into
        // "Something has gone wrong". The original request is the page to return to, in either language.
        var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        return reExecute is null
            ? $"~{Request.Path}{Request.QueryString}"
            : $"~{reExecute.OriginalPath}{reExecute.OriginalQueryString}";
    }
}