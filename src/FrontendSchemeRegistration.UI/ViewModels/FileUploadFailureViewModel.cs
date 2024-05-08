namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadFailureViewModel
{
    public string FileName { get; set; }

    public Guid SubmissionId { get; set; }

    public int MaxErrorsToProcess { get; set; }

    public bool HasWarnings { get; set; }

    public string MaxReportSize { get; set; }
}