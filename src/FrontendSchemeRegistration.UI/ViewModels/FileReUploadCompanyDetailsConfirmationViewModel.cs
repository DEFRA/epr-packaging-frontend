namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class FileReUploadCompanyDetailsConfirmationViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    public string CompanyDetailsFileName { get; set; }

    public string CompanyDetailsFileUploadDate { get; set; }

    public string CompanyDetailsFileUploadedBy { get; set; }

    public bool IsCompanyDetailsFileUploadedByDeleted { get; set; }

    public string? PartnersFileName { get; set; }

    public string? PartnersFileUploadDate { get; set; }

    public string? PartnersFileUploadedBy { get; set; }

    public bool IsPartnersFileUploadedByDeleted { get; set; }

    public string? BrandsFileName { get; set; }

    public string? BrandsFileUploadDate { get; set; }

    public string? BrandsFileUploadedBy { get; set; }

    public bool IsBrandsFileUploadedByDeleted { get; set; }

    public string SubmissionDeadline { get; set; }

    public bool IsApprovedUser { get; set; }

    public bool IsSubmitted { get; set; }

    public bool HasValidfile { get; set; }

    public SubmissionPeriodStatus Status { get; set; }
}