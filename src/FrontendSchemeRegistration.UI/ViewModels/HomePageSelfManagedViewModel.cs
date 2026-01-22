using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.DTOs.ComplianceScheme;
using Shared;

[ExcludeFromCodeCoverage]
public class HomePageSelfManagedViewModel
{
    public string OrganisationName { get; set; } = string.Empty;

    public string OrganisationNumber { get; set; } = string.Empty;

    public bool CanSelectComplianceScheme { get; set; }

    public string OrganisationRole { get; set; } = string.Empty;

    /// <summary>
    /// Obligation year
    /// </summary>
    public string ComplianceYear { get; set; }

    public string LastYear => (DateTime.Now.Year - 1).ToString();

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();

    public ResubmissionTaskListViewModel ResubmissionTaskListViewModel { get; set; }
    public List<RegistrationApplicationViewModel> RegistrationApplicationsPerYear { get; set; } = new();
    public IEnumerable<RegistrationApplicationViewModel> RegistrationApplications { get; set; } = []; 

    public SubmissionPeriod PackagingResubmissionPeriod { get; set; }
}

[ExcludeFromCodeCoverage]
public class RegisterYourMembersViewModel
{
    public string RegisterYourMembersLink = "MembersRegistrationSubmissionPeriodSelection";
    public ComplianceSchemeSummary ComplianceSchemeSummary { get; set; }//carried over for nation data
}
