namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileDownloadViewModel
{
    [Required]
    public Guid SubmissionId { get; set; }
}
