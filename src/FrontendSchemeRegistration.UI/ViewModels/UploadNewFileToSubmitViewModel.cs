namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class UploadNewFileToSubmitViewModel : ViewModelWithOrganisationRole
{
    public Status Status { get; set; }

    public bool IsApprovedOrDelegatedUser { get; set; }

    public Guid SubmissionId { get; set; }

    public string? UploadedFileName { get; set; }

    public DateTime? UploadedAt { get; set; }

    public string? UploadedBy { get; set; }

    public string? SubmittedBy { get; set; }

    public bool IsUploadByPersonDeleted { get; set; }

    public bool IsSubmittedByPersonDeleted { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? SubmittedFileName { get; set; }

    public bool HasNewFileUploaded { get; set; }

    public string? RegulatorComment { get; set; }

    public string RegulatorDecision { get; set; }

    public bool IsResubmissionNeeded { get; set; }
}