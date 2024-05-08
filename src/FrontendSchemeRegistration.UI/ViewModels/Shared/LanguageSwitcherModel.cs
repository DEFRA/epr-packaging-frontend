namespace FrontendSchemeRegistration.UI.ViewModels.Shared;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

[ExcludeFromCodeCoverage]
public class LanguageSwitcherModel
{
    public CultureInfo CurrentCulture { get; set; }

    public List<CultureInfo> SupportedCultures { get; set; }

    public string ReturnUrl { get; set; }

    public bool ShowLanguageSwitcher { get; set; }
}