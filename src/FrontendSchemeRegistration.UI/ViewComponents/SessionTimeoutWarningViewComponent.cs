namespace FrontendSchemeRegistration.UI.ViewComponents;

using Constants;
using FrontendSchemeRegistration.UI.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SessionTimeoutWarningViewComponent : ViewComponent
{
    private readonly IFeatureManager _featureManager;

    public SessionTimeoutWarningViewComponent(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var sessionTimeoutWarningModel = new SessionTimeoutWarningModel
        {
            ShowSessionTimeoutWarning = await _featureManager.IsEnabledAsync(FeatureFlags.ShowSessionTimeoutWarning)

        };

        return View(sessionTimeoutWarningModel);
    }
}