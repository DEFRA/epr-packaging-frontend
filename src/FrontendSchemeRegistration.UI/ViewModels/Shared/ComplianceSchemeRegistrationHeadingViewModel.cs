namespace FrontendSchemeRegistration.UI.ViewModels.Shared;

using Application.Enums;
using Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// If the heading does not require localising (such as a company name, use the Heading argument, otherwise, use the LocalizedHeading argument (by using the Localizer)"/>
/// </summary>
public record ComplianceSchemeRegistrationHeadingViewModel(bool ShowRegistrationCaption, RegistrationJourney? RegistrationJourney, string? Heading, LocalizedHtmlString? LocalizedHeading, int? RegistrationYear);