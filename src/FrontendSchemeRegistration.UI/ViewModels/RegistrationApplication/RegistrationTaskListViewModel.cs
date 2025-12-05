using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

using Application.Enums;

[ExcludeFromCodeCoverage]
public class RegistrationTaskListViewModel
{
    public bool IsResubmission { get; set; }
    
    public string OrganisationName { get; set; } = string.Empty;

    public string OrganisationNumber { get; set; } = string.Empty;

    public bool IsComplianceScheme { get; set; }
    
    public ApplicationStatusType ApplicationStatus { get; set; }

    public RegistrationTaskListStatus FileUploadStatus { get; set; }

    public RegistrationTaskListStatus PaymentViewStatus { get; set; }

    public RegistrationTaskListStatus AdditionalDetailsStatus { get; set; }

    public int RegistrationYear { get; set; }
    public bool ShowRegistrationCaption { get; set; } = false;

    public string PageTitle => IsResubmission ? "registration_resubmission_task_title" : "registration_task_title";
    
    public string PageHeading => IsResubmission ? "register_resubmission_company" : "register_company";
    public string PageHeadingCsoLarge => "register_company_size_large";
    public string PageHeadingCsoSmall => "register_company_size_small";
    
    public string FileUploadTask => IsResubmission ? "update_registration_details" : "submit_registration_data";
    
    public string CompletedFileUploadTask => IsResubmission ? "you_have_resubmitted_organisation_brand_partner_details" : "you_have_submitted_organisation_brand_partner_details";

    public string ViewPaymentTask => IsResubmission ? "view_pay_fee_if_required" : "view_pay_registration_fee";
    
    public string CompletedViewPaymentTask => IsResubmission ? "you_have_paid_resubmission_fee" : "you_have_paid_registration_fee";

    public string SubmitRegistrationApplicationTask => IsResubmission ? "submit_amended_data" : "submit_registration_application";
    
    public string CompletedSubmitRegistrationApplicationTask => IsResubmission ? "registration_resubmission_application_has_been_submitted" : "registration_application_has_been_submitted";
    public RegistrationJourney? RegistrationJourney { get; set; }
}