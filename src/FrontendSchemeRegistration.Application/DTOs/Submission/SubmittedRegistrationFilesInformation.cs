namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmittedRegistrationFilesInformation
{
    public string CompanyDetailsFileName { get; set; } = string.Empty;

    public string BrandsFileName { get; set; } = string.Empty;

    public string PartnersFileName { get; set; } = string.Empty;

    public DateTime? SubmittedDateTime { get; set; }

    public Guid? SubmittedBy { get; set; }
}