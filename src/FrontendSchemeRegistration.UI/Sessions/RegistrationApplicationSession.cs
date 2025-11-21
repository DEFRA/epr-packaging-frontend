using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;

namespace FrontendSchemeRegistration.UI.Sessions;

using Application.DTOs;
using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationSession
{
    public string? SubmissionPeriod { get; set; }

    public SubmissionPeriod Period { get; set; }

    public List<string> Journey { get; set; } = [];

    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsResubmission { get; set; }

    public ApplicationStatusType ApplicationStatus { get; set; }

    public RegistrationTaskListStatus FileUploadStatus
    {
        get
        {
            if (ApplicationStatus is
                ApplicationStatusType.CancelledByRegulator
                or ApplicationStatusType.QueriedByRegulator
                or ApplicationStatusType.RejectedByRegulator
               )
            {
                return RegistrationTaskListStatus.NotStarted;
            }

            if (ApplicationStatus is ApplicationStatusType.SubmittedToRegulator)
            {
                return FileReachedSynapse ? RegistrationTaskListStatus.Completed : RegistrationTaskListStatus.Pending;
            }

            if (ApplicationStatus is
                ApplicationStatusType.AcceptedByRegulator
                or ApplicationStatusType.ApprovedByRegulator
               )
            {
                return RegistrationTaskListStatus.Completed;
            }

            if (!FileReachedSynapse && ApplicationStatus is
                    ApplicationStatusType.FileUploaded or
                    ApplicationStatusType.SubmittedAndHasRecentFileUpload)
            {
                return RegistrationTaskListStatus.Pending;
            }

            return RegistrationTaskListStatus.NotStarted;
        }
    }

    public RegistrationTaskListStatus PaymentViewStatus
    {
        get
        {
            if (FileUploadStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.Pending)
            {
                return RegistrationTaskListStatus.CanNotStartYet;
            }

            if (FileUploadStatus is RegistrationTaskListStatus.Completed && !IsRegistrationFeePaid)
            {
                return RegistrationTaskListStatus.NotStarted;
            }

            if (FileUploadStatus == RegistrationTaskListStatus.Completed && IsRegistrationFeePaid)
            {
                return RegistrationTaskListStatus.Completed;
            }

            return RegistrationTaskListStatus.NotStarted;
        }
    }

    public RegistrationTaskListStatus AdditionalDetailsStatus
    {
        get
        {
            if (PaymentViewStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.Pending)
            {
                return RegistrationTaskListStatus.CanNotStartYet;
            }

            if (PaymentViewStatus is RegistrationTaskListStatus.Completed && !RegistrationApplicationSubmitted)
            {
                return RegistrationTaskListStatus.NotStarted;
            }

            if (PaymentViewStatus == RegistrationTaskListStatus.Completed && RegistrationApplicationSubmitted)
            {
                return RegistrationTaskListStatus.Completed;
            }

            return RegistrationTaskListStatus.CanNotStartYet;
        }
    }

    public bool IsRegistrationFeePaid => RegistrationFeePaymentMethod is "PayByPhone" or "PayOnline" or "PayByBankTransfer" or "No-Outstanding-Payment";

    public bool RegistrationApplicationSubmitted => RegistrationApplicationSubmittedDate is not null;

    public bool FileReachedSynapse => RegistrationFeeCalculationDetails is { Length: > 0 };

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
    public ProducerSize? ProducerSize { get; set; }
}