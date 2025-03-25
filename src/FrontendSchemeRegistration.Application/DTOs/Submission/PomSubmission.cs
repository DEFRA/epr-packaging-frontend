namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Diagnostics.CodeAnalysis;
using Enums;

[ExcludeFromCodeCoverage]
public class PomSubmission : AbstractSubmission
{
    public override SubmissionType Type => SubmissionType.Producer;

    public string PomFileName { get; set; }

    public DateTime? PomFileUploadDateTime { get; set; }

    public bool PomDataComplete { get; set; }

	public string? AppReferenceNumber { get; set; }

	public bool? IsResubmissionInProgress { get; set; }

    public bool? IsResubmissionComplete { get; set; }

    public UploadedFileInformation? LastUploadedValidFile { get; set; }

    public SubmittedFileInformation? LastSubmittedFile { get; set; }
}