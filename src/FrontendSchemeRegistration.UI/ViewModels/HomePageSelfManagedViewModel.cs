namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class HomePageSelfManagedViewModel
{
    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public bool CanSelectComplianceScheme { get; set; }

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();
}