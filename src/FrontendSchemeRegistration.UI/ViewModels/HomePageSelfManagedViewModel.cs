namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class HomePageSelfManagedViewModel
{
    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public bool CanSelectComplianceScheme { get; set; }

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();

    public List<SubmissionPeriod> SubmissionPeriods { get; set; } = new List<SubmissionPeriod>();
}