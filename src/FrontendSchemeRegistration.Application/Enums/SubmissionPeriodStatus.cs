using FrontendSchemeRegistration.Application.Attributes;

namespace FrontendSchemeRegistration.Application.Enums;

public enum SubmissionPeriodStatus
{
    [LocalizedName("not_started")]
    NotStarted,
    [LocalizedName("file_uploaded")]
    FileUploaded,
    [LocalizedName("submitted_to_regulator")]
    SubmittedToRegulator,
    [LocalizedName("cannot_start_yet")]
    CannotStartYet,
    [LocalizedName("submitted_to_regulator")]
    SubmittedAndHasRecentFileUpload,
    [LocalizedName("accepted_by_regulator")]
    AcceptedByRegulator,
    [LocalizedName("rejected_by_regulator")]
    RejectedByRegulator,
    [LocalizedName("approved_by_regulator")]
    ApprovedByRegulator
}