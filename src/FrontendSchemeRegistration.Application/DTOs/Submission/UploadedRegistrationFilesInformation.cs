using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class UploadedRegistrationFilesInformation
{
    public Guid CompanyDetailsFileId { get; set; }

    public string CompanyDetailsFileName { get; set; } = string.Empty;

    public Guid CompanyDetailsUploadedBy { get; set; }

    public DateTime CompanyDetailsUploadDatetime { get; set; }

    public string BrandsFileName { get; set; } = string.Empty;

    public Guid? BrandsUploadedBy { get; set; }

    public DateTime? BrandsUploadDatetime { get; set; }

    public string PartnershipsFileName { get; set; } = string.Empty;

    public Guid? PartnershipsUploadedBy { get; set; }

    public DateTime? PartnershipsUploadDatetime { get; set; }
}