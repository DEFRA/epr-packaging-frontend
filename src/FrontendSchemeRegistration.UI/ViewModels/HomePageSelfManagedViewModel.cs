namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class HomePageSelfManagedViewModel
{
    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public bool CanSelectComplianceScheme { get; set; }

    public string OrganisationRole { get; set; }

    public string CurrentYear => (DateTime.Now.Year).ToString();

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();
}