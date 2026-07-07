using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Domain;
using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

public class RegistrationTaskListDomainViewModel(
    Registration registration,
    string organisationName,
    string organisationNumber,
    int registrationYear)
{
    public string OrganisationName { get; } = organisationName;
    public string OrganisationNumber { get; } = organisationNumber;
    public int RegistrationYear { get; } = registrationYear;

    public bool IsResubmission => registration.IsResubmission;
    public bool IsComplianceScheme => registration.IsComplianceScheme;
    public bool ShowRegistrationCaption => registration.RegistrationJourney is not null;
    public RegistrationJourney? RegistrationJourney => registration.RegistrationJourney;
    public RegistrationTaskListStatus FileUploadStatus => registration.FileUploadStatus;
    public RegistrationTaskListStatus PaymentViewStatus => registration.PaymentViewStatus;
    public RegistrationTaskListStatus AdditionalDetailsStatus => registration.AdditionalDetailsStatus;

    public bool ShowFileUploadLink =>
        registration.FileUploadStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.CanNotStartYet
        || registration.ApplicationStatus is ApplicationStatusType.FileUploaded or ApplicationStatusType.SubmittedAndHasRecentFileUpload;

    public bool FileUploadCompleted => registration.FileUploadStatus == RegistrationTaskListStatus.Completed;
    public bool CanStartPaymentStep => registration.PaymentViewStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.Completed;
    public bool PaymentCompleted => registration.PaymentViewStatus == RegistrationTaskListStatus.Completed;
    public bool CanStartSubmitStep => registration.AdditionalDetailsStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.Completed;
    public bool SubmitApplicationCompleted => registration.AdditionalDetailsStatus == RegistrationTaskListStatus.Completed;

    public string PageTitle => IsResubmission ? "registration_resubmission_task_title" : "registration_task_title";
    public string PageHeading => IsResubmission ? "register_resubmission_company" : "register_company";
    public string FileUploadTask => IsResubmission ? "update_registration_details" : "submit_registration_data";
    public string CompletedFileUploadTask => IsResubmission ? "you_have_resubmitted_registration_details" : "you_have_submitted_registration_details";
    public string ViewPaymentTask => IsResubmission ? "view_pay_fee_if_required" : "view_pay_registration_fee";
    public string CompletedViewPaymentTask => IsResubmission ? "you_have_paid_resubmission_fee" : "you_have_paid_registration_fee";
    public string SubmitRegistrationApplicationTask => IsResubmission ? "submit_amended_data" : "submit_registration_application";
    public string CompletedSubmitRegistrationApplicationTask => IsResubmission ? "registration_resubmission_application_has_been_submitted" : "registration_application_has_been_submitted";
}
