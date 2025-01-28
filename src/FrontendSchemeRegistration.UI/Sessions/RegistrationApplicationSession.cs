using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Sessions;

using Application.DTOs;
using Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationSession
{
    public RegistrationTaskListStatus FileUploadStatus
    {
        get
        {
            if (ApplicationStatus is
                    ApplicationStatusType.SubmittedToRegulator
                    or ApplicationStatusType.AcceptedByRegulator 
                    or ApplicationStatusType.ApprovedByRegulator
                    or ApplicationStatusType.CancelledByRegulator
                    or ApplicationStatusType.QueriedByRegulator
                    or ApplicationStatusType.RejectedByRegulator
                    )
            {
                return FileReachedSynapse ? RegistrationTaskListStatus.Completed : RegistrationTaskListStatus.Pending;
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
            if (FileUploadStatus == RegistrationTaskListStatus.NotStarted || FileUploadStatus == RegistrationTaskListStatus.Pending)
            {
                return RegistrationTaskListStatus.CanNotStartYet;
            }

            if (FileUploadStatus is RegistrationTaskListStatus.Completed && !RegistrationFeePaid)
            {
                return RegistrationTaskListStatus.NotStarted;
            }

            if (FileUploadStatus == RegistrationTaskListStatus.Completed && RegistrationFeePaid)
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
            if (ApplicationStatus is ApplicationStatusType.AcceptedByRegulator || (PaymentViewStatus is RegistrationTaskListStatus.Completed && RegistrationApplicationSubmitted))
            {
                return RegistrationTaskListStatus.Completed;
            }

            if (PaymentViewStatus is RegistrationTaskListStatus.NotStarted or RegistrationTaskListStatus.CanNotStartYet)
            {
                return RegistrationTaskListStatus.CanNotStartYet;
            }

            if (PaymentViewStatus is RegistrationTaskListStatus.Completed && !RegistrationApplicationSubmitted)
            {
                return RegistrationTaskListStatus.NotStarted;
            }

            return RegistrationTaskListStatus.NotStarted;
        }
    }

    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public string? RegistrationReferenceNumber { get; set; } = string.Empty;

    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();

    public string? RegistrationFeePaymentMethod { get; set; }

    public bool RegistrationFeePaid => RegistrationFeePaymentMethod is "PayByPhone" or "PayOnline" or "PayByBankTransfer";

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }

    public bool RegistrationApplicationSubmitted => RegistrationApplicationSubmittedDate is not null;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public bool FileReachedSynapse { get; set; }

    public ProducerDetailsDto? ProducerDetails { get; set; }

    public ComplianceSchemeDetailsDto? CsoMemberDetails { get; set; }

    public int TotalAmountOutstanding { get; set; }

    public string RegulatorNation { get; set; } = string.Empty;

    public bool IsLateFeeApplicable { get; set; }
}