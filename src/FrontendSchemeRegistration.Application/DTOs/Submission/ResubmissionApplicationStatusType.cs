namespace FrontendSchemeRegistration.Application.DTOs.Submission;

public enum ResubmissionApplicationStatusType
{
    NotStarted,
    FileUploaded,
    SubmittedAndHasRecentFileUpload,
    SubmittedToRegulator,
    AcceptedByRegulator,
    RejectedByRegulator,
    ApprovedByRegulator,
    CancelledByRegulator,
    QueriedByRegulator
}