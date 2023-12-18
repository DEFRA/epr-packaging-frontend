namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.DTOs.Submission;

public class HomePageSelfManagedViewModel
{
    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public bool CanSelectComplianceScheme { get; set; }

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();

    public List<SubmissionPeriod> SubmissionPeriods { get; set; } = new List<SubmissionPeriod>();
}