namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeLandingViewModel
{
    public Guid CurrentComplianceSchemeId { get; set; }

    public string OrganisationName { get; set; }

    public List<ComplianceSchemeDto> ComplianceSchemes { get; set; }

    public ComplianceSchemeSummary CurrentTabSummary { get; set; }

    public NotificationViewModel Notification { get; set; } = new();

    public List<DatePeriod> SubmissionPeriods { get; set; } = new();

    public string CurrentYear => (DateTime.Now.Year).ToString();

    public bool IsApprovedUser { get; set; }

    public ApplicationStatusType? ApplicationStatus { get; set; }
    public RegistrationTaskListStatus? FileUploadStatus { get; set; } = RegistrationTaskListStatus.NotStarted;

    public RegistrationTaskListStatus? PaymentViewStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;

    public RegistrationTaskListStatus? AdditionalDetailsStatus { get; set; } = RegistrationTaskListStatus.CanNotStartYet;

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