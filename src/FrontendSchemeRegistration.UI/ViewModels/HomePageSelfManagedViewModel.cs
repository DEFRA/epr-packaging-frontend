using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.ViewModels;


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
    public List<RegistrationApplicationPerYearViewModel> RegistrationApplicationsPerYear { get; set; } = new();

    public SubmissionPeriod PackagingResubmissionPeriod { get; set; }
}

[ExcludeFromCodeCoverage]
public class RegistrationApplicationPerYearViewModel
{
    public bool IsComplianceScheme { get; set; }
    public string RegistrationYear { get; set; } = default!;
    public RegistrationTaskListStatus FileUploadStatus { get; set; } = RegistrationTaskListStatus.NotStarted;
    public RegistrationTaskListStatus PaymentViewStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;
    public RegistrationTaskListStatus AdditionalDetailsStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;
    public string? ApplicationReferenceNumber { get; set; }
    public string? RegistrationReferenceNumber { get; set; }
    public bool IsResubmission { get; set; }
    public ApplicationStatusType ApplicationStatus { get; set; }
    public bool showLargeProducer { get; set; }
    public bool feature_AlwaysShowLargeProducerJourneyMessage { get; set; }
    public bool RegisterSmallProducersCS { get; set; }
    public string CurrentYear => (DateTime.Now.Year).ToString();
    public string RegistrationApplicationLink
       => ApplicationStatus is
              ApplicationStatusType.FileUploaded
              or ApplicationStatusType.SubmittedAndHasRecentFileUpload
              or ApplicationStatusType.CancelledByRegulator
              or ApplicationStatusType.QueriedByRegulator
              or ApplicationStatusType.RejectedByRegulator
          || FileUploadStatus is
              RegistrationTaskListStatus.Pending
              or RegistrationTaskListStatus.Completed
           ? "RegistrationTaskList"
           : "ProducerRegistrationGuidance";
}
