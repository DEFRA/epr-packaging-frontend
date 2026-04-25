namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

public class LandingPageViewModel
{
    public Guid OrganisationId { get; set; }

    public string OrganisationName { get; set; }

    public string? OrganisationNumber { get; set; }

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();
}