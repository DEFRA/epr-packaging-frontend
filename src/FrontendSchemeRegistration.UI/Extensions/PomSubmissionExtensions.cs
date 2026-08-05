namespace FrontendSchemeRegistration.UI.Extensions;

using Application.DTOs.Submission;

public static class PomSubmissionExtensions
{
    /// <summary>
    /// True when a more recent upload attempt exists than the file currently offered for submission.
    /// </summary>
    /// <remarks>
    /// SUB-332: <c>PomFileUploadDateTime</c> comes from the latest antivirus check event, which is written
    /// for every upload regardless of outcome, whereas <c>LastUploadedValidFile</c> falls back to an older
    /// file when the latest one does not validate. A gap between the two means the user's most recent
    /// attempt never became the valid file - because it failed validation, or because validation never
    /// completed - and that they would otherwise be shown the earlier file with nothing to indicate why.
    /// </remarks>
    public static bool HasNewerUnprocessedUploadThanValidFile(this PomSubmission? submission) =>
        submission?.LastUploadedValidFile is not null &&
        submission.PomFileUploadDateTime > submission.LastUploadedValidFile.FileUploadDateTime;
}
