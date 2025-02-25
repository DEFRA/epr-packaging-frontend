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

    public string CurrentYear => (DateTime.Now.Year).ToString();

    public NotificationViewModel Notification { get; set; } = new NotificationViewModel();

    public RegistrationTaskListStatus FileUploadStatus { get; set; } = RegistrationTaskListStatus.NotStarted;

    public RegistrationTaskListStatus PaymentViewStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;

    public RegistrationTaskListStatus AdditionalDetailsStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public string? ApplicationReferenceNumber { get; set; }

    public string? RegistrationReferenceNumber { get; set; }

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