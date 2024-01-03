namespace FrontendSchemeRegistration.UI.ViewModels;

public class FileUploadWarningViewModel
{
    public string FileName { get; set; }

    public Guid SubmissionId { get; set; }

    public int MaxWarningsToProcess { get; set; }

    public string MaxReportSize { get; set; }
}