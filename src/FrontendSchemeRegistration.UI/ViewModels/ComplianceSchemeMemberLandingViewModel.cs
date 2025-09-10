namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeMemberLandingViewModel
{
    public string ComplianceSchemeName { get; set; }

    public string OrganisationName { get; set; }

    public Guid OrganisationId { get; set; }

    public string OrganisationNumber { get; set; }

    public string ServiceRole { get; set; }

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();
}