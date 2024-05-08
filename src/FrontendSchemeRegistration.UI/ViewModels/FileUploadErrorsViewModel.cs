namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadErrorsViewModel
{
    public int ErrorCount { get; set; }

    public string? OrganisationRole { get; set; }

    public DateTime SubmissionDeadline { get; set; }

    public Guid SubmissionId { get; set; }
}