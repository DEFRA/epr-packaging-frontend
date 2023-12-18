namespace FrontendSchemeRegistration.UI.ViewModels;

public class FileUploadSuccessViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    public string FileName { get; set; }

    public DateTime SubmissionDeadline { get; set; }
}
