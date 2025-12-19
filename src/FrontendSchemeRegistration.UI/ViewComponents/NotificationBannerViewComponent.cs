namespace FrontendSchemeRegistration.UI.ViewComponents;

using System.Diagnostics.CodeAnalysis;
using Application.Options;
using Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using ViewModels.Shared.Components.NotificationBanner;

/// <summary>
///     A view component for displaying a configurable notification banner.
///     Notifications are configured via App Settings.
/// </summary>
public class NotificationBannerViewComponent(
    IFeatureManagerSnapshot featureManager,
    IOptionsSnapshot<NotificationBannerOptions> featureOptions,
    TimeProvider timeProvider)
    : ViewComponent
{
    private const string FeatureFlag = FeatureFlags.ShowNotificationBanner;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Feature flag controls the entire banner regardless of any other settings.
        if (!await featureManager.IsEnabledAsync(FeatureFlag))
            return Content(string.Empty);

        var model = GetModel();

        if (model is null)
            return Content(string.Empty);

        return View(model);
    }

    private NotificationBannerViewModel? GetModel()
    {
        if (!TryGetActiveNotification(out var notification))
            return null;

        return new NotificationBannerViewModel
        {
            TitleResource = $"{notification.ResourceKey}_title",
            HeaderResource = $"{notification.ResourceKey}_header",
            BodyResource = $"{notification.ResourceKey}_body"
        };
    }

    private bool TryGetActiveNotification(
        [NotNullWhen(true)] out NotificationBannerOptions.NotificationOption? active)
    {
        var options = featureOptions.Value;
        var now = timeProvider.GetUtcNow();

        // Since it's possible for time window overlaps to occur, we'll prioritize whichever has the latest EffectiveFrom.
        active = options.Notifications
            .Where(m => m.EffectiveFrom <= now && m.EffectiveTo >= now)
            .MaxBy(m => m.EffectiveFrom);

        return active != null;
    }
}