namespace FrontendSchemeRegistration.UI.ViewModels;

public class LandingPageViewModel
{
    public Guid OrganisationId { get; set; }

    public string OrganisationName { get; set; }

    public string? OrganisationNumber { get; set; }

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();
}