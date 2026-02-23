namespace FrontendSchemeRegistration.UI.ViewModels.Shared;

using Application.Enums;
using Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// If the heading does not require localising (such as a company name, use the Heading argument, otherwise, use the LocalizedHeading argument (by using the Localizer)"/>
/// ShowRegistrationCaption determines whether the title such as Small Producer 2026 registration is displayed above the main h1 heading 
/// </summary>
public record ComplianceSchemeRegistrationHeadingViewModel(
    bool ShowRegistrationCaption,
    RegistrationJourney? RegistrationJourney,
    string? Heading, 
    LocalizedHtmlString? LocalizedHeading,
    int? RegistrationYear);
