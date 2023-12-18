namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmittedFileInformation
{
    public Guid FileId { get; set; }

    public string FileName { get; set; }

    public DateTime SubmittedDateTime { get; set; }

    public Guid SubmittedBy { get; set; }
}