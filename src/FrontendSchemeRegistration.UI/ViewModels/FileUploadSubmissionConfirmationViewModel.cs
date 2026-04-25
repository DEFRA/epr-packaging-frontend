namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

public class FileUploadSubmissionConfirmationViewModel : ViewModelWithOrganisationRole
{
    public string SubmittedBy { get; set; }

    public DateTime SubmittedAt { get; set; }
}
