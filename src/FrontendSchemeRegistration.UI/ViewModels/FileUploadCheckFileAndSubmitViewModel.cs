namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;

public class FileUploadCheckFileAndSubmitViewModel : ViewModelWithOrganisationRole
{
    public Guid? SubmissionId { get; set; }

    public bool UserCanSubmit { get; set; }

    [Required]
    public Guid? LastValidFileId { get; set; }

    public string? LastValidFileName { get; set; }

    public DateTime? LastValidFileUploadDateTime { get; set; }

    public string? LastValidFileUploadedBy { get; set; }

    public string? SubmittedBy { get; set; }

    public DateTime? SubmittedDateTime { get; set; }

    public string? SubmittedFileName { get; set; }

    public bool HasSubmittedPreviously => SubmittedFileName is not null;

    public bool IsSubmittedByUserDeleted { get; set; }

    // SUB-332: a more recent upload attempt that never became the valid file. When true, submission is
    // blocked outright rather than warned about - the file on offer is not the one the user believes they
    // are submitting, so declaring it would send the wrong data to the regulator.
    public bool HasNewerUnprocessedUpload { get; set; }

    public string? UnprocessedUploadFileName { get; set; }

    public DateTime? UnprocessedUploadDateTime { get; set; }
}