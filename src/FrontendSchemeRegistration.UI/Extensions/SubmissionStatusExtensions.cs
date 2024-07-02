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

    public static SubmissionPeriodStatus GetSubmissionStatus(this RegistrationSubmission submission, SubmissionPeriod submissionPeriod,
    RegistrationDecision decision,
    bool showRegistrationDecision)
    {
        if (DateTime.Now < submissionPeriod.ActiveFrom)
        {
            return SubmissionPeriodStatus.CannotStartYet;
        }

        if (submission is null)
        {
            return SubmissionPeriodStatus.NotStarted;
        }

        if (submission.IsSubmitted)
        {
            if (!showRegistrationDecision)
            {
                return submission.LastUploadedValidFiles?.CompanyDetailsUploadDatetime >
                       submission.LastSubmittedFiles?.SubmittedDateTime ? SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload : SubmissionPeriodStatus.SubmittedToRegulator;
            }

            switch (decision.Decision)
            {
                case "Accepted":
                case "Approved":
                    return SubmissionPeriodStatus.AcceptedByRegulator;
                case "Rejected":
                    return SubmissionPeriodStatus.RejectedByRegulator;
                default:
                    return submission.LastUploadedValidFiles?.CompanyDetailsUploadDatetime >
                      submission.LastSubmittedFiles?.SubmittedDateTime ? SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload : SubmissionPeriodStatus.SubmittedToRegulator;
            }
        }

        return submission.LastUploadedValidFiles != null ? SubmissionPeriodStatus.FileUploaded : SubmissionPeriodStatus.NotStarted;
    }
}