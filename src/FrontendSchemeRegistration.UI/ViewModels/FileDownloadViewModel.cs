namespace FrontendSchemeRegistration.UI.ViewModels;

using FrontendSchemeRegistration.UI.Constants;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileDownloadViewModel
{
    [Required]
    public Guid SubmissionId { get; set; }

    public string Type { get; set; } = FileDownloadType.Upload;
}
