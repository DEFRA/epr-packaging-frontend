namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Resources;

[ExcludeFromCodeCoverage]
public class FileUploadWarningViewModel
{
    public string FileName { get; set; }

    [Required(ErrorMessageResourceName = "select_yes_if_you_want_to_upload_a_new_file", ErrorMessageResourceType = typeof(ErrorMessages))]
    public bool? UploadNewFile { get; set; }

    public Guid SubmissionId { get; set; }

    public int MaxWarningsToProcess { get; set; }

    public string MaxReportSize { get; set; }
}