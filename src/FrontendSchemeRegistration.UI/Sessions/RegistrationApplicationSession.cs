using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.UI.Helpers;

namespace FrontendSchemeRegistration.UI.Sessions;

using Application.DTOs;
using Application.DTOs.Submission;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationSession
{
    public string? SubmissionPeriod { get; set; }

    public SubmissionPeriod Period { get; set; }

    public List<string> Journey { get; set; } = [];

    public Guid? SubmissionId { get; set; }

    // Has this been submitted for fee calculation (submit 1)
    public bool IsSubmitted { get; set; }

    public bool IsResubmission { get; set; }

    public ApplicationStatusType ApplicationStatus { get; set; }

    public RegistrationTaskListStatus FileUploadStatus =>
        RegistrationApplicationStatusCalculator.CalculateFileUploadStatus(ApplicationStatus, ReadyToCalculateFees);

    public RegistrationTaskListStatus PaymentViewStatus =>
        RegistrationApplicationStatusCalculator.CalculatePaymentViewStatus(FileUploadStatus, IsRegistrationFeePaid);

    public RegistrationTaskListStatus AdditionalDetailsStatus =>
        RegistrationApplicationStatusCalculator.CalculateAdditionalDetailsStatus(PaymentViewStatus, RegistrationApplicationSubmitted);

    public bool IsRegistrationFeePaid => RegistrationApplicationStatusCalculator.IsRegistrationFeePaid(RegistrationFeePaymentMethod);

    public bool RegistrationApplicationSubmitted => RegistrationApplicationSubmittedDate is not null;

    public bool ReadyToCalculateFees => RegistrationApplicationStatusCalculator.ReadyToCalculateFees(RegistrationFeeCalculationDetails);

    // this is the last time the submission was submitted to the Regulator for review (submit 2)
    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }

    public string? RegistrationFeePaymentMethod { get; set; }

    public bool IsLateFeeApplicable { get; set; }

    public bool IsOriginalCsoSubmissionLate { get; set; }

    public bool HasAnyApprovedOrQueriedRegulatorDecision { get; set; }

    // is the most recent submission (submit 1) after the latest successful (and validated) file upload? If not,
    // then the file has been successfully uploaded, but not submitted (submit 1), and is therefore ready to submit (submit 1)
    public bool IsLatestSubmittedEventAfterFileUpload { get; set; }

    // this is the last time that the file was submitted for fee calculation (submit 1)
    public DateTime? LatestSubmittedEventCreatedDatetime { get; set; }

    // this is the first time the submission was submitted to the Regulator for review (submit 2)
    public DateTime? FirstApplicationSubmittedEventCreatedDatetime { get; set; }

    public int TotalAmountOutstanding { get; set; }

    // This is generated when the application is submitted for fee calculation (submit 1), eg PEPR1690421049026P1L
    public string? ApplicationReferenceNumber { get; set; }

    // This is generated when the registration is granted by the regulator, eg R26EC169042104392L
    public string? RegistrationReferenceNumber { get; set; }

    public string RegulatorNation { get; set; } = string.Empty;

    public bool IsComplianceScheme => SelectedComplianceScheme is not null;

    public RegistrationFeeCalculationDetails[]? RegistrationFeeCalculationDetails { get; set; }

    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();

    public ComplianceSchemeDto? SelectedComplianceScheme { get; set; }
    public RegistrationJourney? RegistrationJourney { get; set; }
    public bool ShowRegistrationCaption => RegistrationJourney != null;
    public bool SkipProducerRegistrationGuidance =>
    ApplicationStatus is
        ApplicationStatusType.FileUploaded
        or ApplicationStatusType.SubmittedAndHasRecentFileUpload
        or ApplicationStatusType.CancelledByRegulator
        or ApplicationStatusType.QueriedByRegulator
        or ApplicationStatusType.RejectedByRegulator
    || FileUploadStatus is
        RegistrationTaskListStatus.Pending
        or RegistrationTaskListStatus.Completed;
}