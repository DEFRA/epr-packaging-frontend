namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using Resources;

public class FileUploadCheckFileAndSubmitViewModel : ViewModelWithOrganisationRole
{
    [Required(ErrorMessageResourceName = "select_yes_if_you_want_to_submit_your_packaging_data", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool? Submit { get; set; }

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
}