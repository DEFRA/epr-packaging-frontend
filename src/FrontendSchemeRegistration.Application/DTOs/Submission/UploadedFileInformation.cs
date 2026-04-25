namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Diagnostics.CodeAnalysis;

public class UploadedFileInformation
{
    public string FileName { get; set; }

    public DateTime FileUploadDateTime { get; set; }

    public Guid UploadedBy { get; set; }

    public Guid FileId { get; set; }
}