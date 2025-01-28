namespace FrontendSchemeRegistration.Application.DTOs.Submission;

public class LastSubmittedFileDetails
{
    public Guid? FileId { get; set; }

    public string? SubmittedByName { get; set; } = string.Empty;

    public DateTime? SubmittedDateTime { get; set; }
}