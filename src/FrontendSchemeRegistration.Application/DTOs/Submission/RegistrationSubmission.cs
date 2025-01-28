namespace FrontendSchemeRegistration.Application.DTOs.Submission;

using System.Diagnostics.CodeAnalysis;
using Enums;

[ExcludeFromCodeCoverage]
public class RegistrationSubmission : AbstractSubmission
{
    public override SubmissionType Type => SubmissionType.Registration;

    public bool BrandsDataComplete { get; set; }

    public string? BrandsFileName { get; set; }

    public Guid? BrandsUploadedBy { get; set; }

    public DateTime? BrandsUploadedDate { get; set; }

    public string CompanyDetailsFileName { get; set; }

    public Guid? CompanyDetailsUploadedBy { get; set; }

    public DateTime? CompanyDetailsUploadedDate { get; set; }

    public bool CompanyDetailsDataComplete { get; set; }

    public bool PartnershipsDataComplete { get; set; }

    public string? PartnershipsFileName { get; set; }

    public Guid? PartnershipsUploadedBy { get; set; }

    public DateTime? PartnershipsUploadedDate { get; set; }

    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public SubmittedRegistrationFilesInformation? LastSubmittedFiles { get; set; }

    public UploadedRegistrationFilesInformation? LastUploadedValidFiles { get; set; }

    public int? RowErrorCount { get; set; }

    public int? OrganisationMemberCount { get; set; }
}