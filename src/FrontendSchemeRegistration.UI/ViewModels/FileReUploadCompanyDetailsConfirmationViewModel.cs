namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.Enums;

public class FileReUploadCompanyDetailsConfirmationViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    public string CompanyDetailsFileName { get; set; }

    public string CompanyDetailsFileUploadDate { get; set; }

    public string CompanyDetailsFileUploadedBy { get; set; }

    public string? PartnersFileName { get; set; }

    public string? PartnersFileUploadDate { get; set; }

    public string? PartnersFileUploadedBy { get; set; }

    public string? BrandsFileName { get; set; }

    public string? BrandsFileUploadDate { get; set; }

    public string? BrandsFileUploadedBy { get; set; }

    public string SubmissionDeadline { get; set; }

    public bool IsApprovedUser { get; set; }

    public bool IsSubmitted { get; set; }

    public bool HasValidfile { get; set; }

    public SubmissionPeriodStatus Status { get; set; }
}
