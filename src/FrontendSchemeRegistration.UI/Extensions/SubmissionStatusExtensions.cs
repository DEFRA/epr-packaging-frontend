namespace FrontendSchemeRegistration.UI.Extensions;

using Application.DTOs.Submission;
using Application.Enums;

public static class SubmissionStatusExtensions
{
    public static SubmissionPeriodStatus GetSubmissionStatus(this RegistrationSubmission submission)
    {
        if (submission.IsSubmitted)
        {
            return submission.LastUploadedValidFiles?.CompanyDetailsUploadDatetime >
                   submission.LastSubmittedFiles?.SubmittedDateTime ? SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload : SubmissionPeriodStatus.SubmittedToRegulator;
        }

        return submission.LastUploadedValidFiles != null ? SubmissionPeriodStatus.FileUploaded : SubmissionPeriodStatus.NotStarted;
    }
}