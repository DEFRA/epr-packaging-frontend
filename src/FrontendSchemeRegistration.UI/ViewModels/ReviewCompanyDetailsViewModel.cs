namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;
using Application.Enums;

public class ReviewCompanyDetailsViewModel : ViewModelWithOrganisationRole, IValidatableObject
{
    private static readonly string[] OrgDetailsResponse = new[] { "SubmitOrganisationDetailsResponse" };

    public Guid SubmissionId { get; set; }

    public string OrganisationDetailsFileName { get; set; }

    public string OrganisationDetailsUploadedBy { get; set; }

    public string OrganisationDetailsFileUploadDate { get; set; }

    public string OrganisationDetailsFileId { get; set; } = string.Empty;

    public string BrandFileName { get; set; } = string.Empty;

    public string BrandUploadedBy { get; set; } = string.Empty;

    public string BrandFileUploadDate { get; set; } = string.Empty;

    public string PartnerFileName { get; set; } = string.Empty;

    public string PartnerUploadedBy { get; set; } = string.Empty;

    public string PartnerFileUploadDate { get; set; } = string.Empty;

    public string RegistrationSubmissionDeadline { get; set; }

    public bool BrandsRequired { get; set; }

    public bool PartnersRequired { get; set; }

    public bool IsApprovedUser { get; set; }

    public bool HasPreviousSubmission { get; set; }

    public bool HasPreviousBrandsSubmission { get; set; }

    public bool HasPreviousPartnersSubmission { get; set; }

    public string SubmittedCompanyDetailsFileName { get; set; } = string.Empty;

    public string SubmittedCompanyDetailsDateTime { get; set; } = string.Empty;

    public string SubmittedBrandsFileName { get; set; } = string.Empty;

    public string SubmittedBrandsDateTime { get; set; } = string.Empty;

    public string SubmittedPartnersFileName { get; set; } = string.Empty;

    public string SubmittedPartnersDateTime { get; set; } = string.Empty;

    public string SubmittedDateTime { get; set; } = string.Empty;

    public string SubmittedBy { get; set; } = string.Empty;

    public bool? SubmitOrganisationDetailsResponse { get; set; }

    public SubmissionPeriodStatus SubmissionStatus { get; set; }
    
    public bool IsResubmission { get; set; }

    public int? RegistrationYear { get; set; }
    public RegistrationJourney? RegistrationJourney { get; set; }
    public bool ShowRegistrationCaption => RegistrationJourney != null && RegistrationYear != null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return ValidateSubmitOrganisationDetails();
    }

    private IEnumerable<ValidationResult> ValidateSubmitOrganisationDetails()
    {
        if (!SubmitOrganisationDetailsResponse.HasValue)
        {
            if (IsComplianceScheme)
            {
                yield return new ValidationResult("ReviewCompanyDetails.ResponseErrorMessage", OrgDetailsResponse);
            }
            else
            {
                yield return new ValidationResult("ReviewCompanyDetails.ResponseErrorMessage.Producer", OrgDetailsResponse);
            }
        }
    }
}