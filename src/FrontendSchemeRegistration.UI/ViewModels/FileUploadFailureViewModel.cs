namespace FrontendSchemeRegistration.UI.ViewModels;

public class FileUploadFailureViewModel
{
    public string FileName { get; set; }

    public Guid SubmissionId { get; set; }

    public int MaxErrorsToProcess { get; set; }
}