namespace FrontendSchemeRegistration.UI.Extensions;

using Microsoft.AspNetCore.Mvc.Localization;

public static class ViewLocalizerExtensions
{
    public static string DecemberWasteFlashText(this IViewLocalizer localizer, int[] availableAcceptanceYears) =>
        availableAcceptanceYears.Length switch
        {
            1 => string.Format(localizer["acceptance_year"].Value, availableAcceptanceYears[0]),
            2 => string.Format(localizer["acceptance_years"].Value, availableAcceptanceYears[0], availableAcceptanceYears[1]),
            _ => string.Empty
        };
}
