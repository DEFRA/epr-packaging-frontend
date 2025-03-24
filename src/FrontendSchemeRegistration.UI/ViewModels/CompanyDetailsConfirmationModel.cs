namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using System.Security.Policy;

[ExcludeFromCodeCoverage]
public class CompanyDetailsConfirmationModel : ViewModelWithOrganisationRole
{
    public string? SubmittedDate { get; set; }

    public string? SubmissionTime { get; set; }

    public string? SubmittedBy { get; set; }

    public bool IsResubmission { get; set; }

    public string RegistrationSubmissionTask => IsResubmission ? "registration_details_updated" : "organisation_details_submitted_registration";

    public string SubmissionTask => IsResubmission ? "updated_by" : "submitted_by";

    public string ViewPaymentTask => IsResubmission ? "view_your_registration_fee" : "pay_your_registration_fee";

    public string SubmitToRegulatorTask => IsResubmission ? "submit_to_environmental_regulator" : "apply_for_registration";

    public string ReturnToRegistrationLink { get; set; }
}