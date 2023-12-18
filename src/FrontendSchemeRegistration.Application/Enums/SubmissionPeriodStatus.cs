namespace FrontendSchemeRegistration.Application.Enums;

using Attributes;

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
    SubmittedAndHasRecentFileUpload
}