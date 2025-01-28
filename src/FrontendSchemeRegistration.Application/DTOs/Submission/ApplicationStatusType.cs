namespace FrontendSchemeRegistration.Application.DTOs.Submission;

public enum ApplicationStatusType
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