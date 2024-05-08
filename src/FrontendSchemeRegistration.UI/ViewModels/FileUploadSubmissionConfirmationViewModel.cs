namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadSubmissionConfirmationViewModel : ViewModelWithOrganisationRole
{
    public string SubmittedBy { get; set; }

    public DateTime SubmittedAt { get; set; }
}
