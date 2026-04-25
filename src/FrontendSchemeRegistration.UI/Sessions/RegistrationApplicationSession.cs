using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.UI.Helpers;

namespace FrontendSchemeRegistration.UI.Sessions;

using Application.DTOs;
using Application.DTOs.Submission;
using Application.Enums;

public class RegistrationApplicationSession
{
    public string? SubmissionPeriod { get; set; }

    public SubmissionPeriod Period { get; set; }

    public List<string> Journey { get; set; } = [];

    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsResubmission { get; set; }

    public ApplicationStatusType ApplicationStatus { get; set; }

    public RegistrationTaskListStatus FileUploadStatus =>
        RegistrationApplicationStatusCalculator.CalculateFileUploadStatus(ApplicationStatus, FileReachedSynapse);

    public RegistrationTaskListStatus PaymentViewStatus =>
        RegistrationApplicationStatusCalculator.CalculatePaymentViewStatus(FileUploadStatus, IsRegistrationFeePaid);

    public RegistrationTaskListStatus AdditionalDetailsStatus =>
        RegistrationApplicationStatusCalculator.CalculateAdditionalDetailsStatus(PaymentViewStatus, RegistrationApplicationSubmitted);

    public bool IsRegistrationFeePaid => RegistrationApplicationStatusCalculator.IsRegistrationFeePaid(RegistrationFeePaymentMethod);

    public bool RegistrationApplicationSubmitted => RegistrationApplicationSubmittedDate is not null;

    public bool FileReachedSynapse => RegistrationApplicationStatusCalculator.FileReachedSynapse(RegistrationFeeCalculationDetails);

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }

    public string? RegistrationFeePaymentMethod { get; set; }


    public bool IsLateFeeApplicable { get; set; }

    public bool IsOriginalCsoSubmissionLate { get; set; }

    public bool HasAnyApprovedOrQueriedRegulatorDecision { get; set; }

    public bool IsLatestSubmittedEventAfterFileUpload { get; set; }

    public DateTime? LatestSubmittedEventCreatedDatetime { get; set; }

    public DateTime? FirstApplicationSubmittedEventCreatedDatetime { get; set; }

    public int TotalAmountOutstanding { get; set; }

    public string? ApplicationReferenceNumber { get; set; }

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