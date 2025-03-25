using System.Diagnostics.CodeAnalysis;
using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.Sessions;

[ExcludeFromCodeCoverage]
public class PackagingResubmissionApplicationSession
{
    public ResubmissionTaskListStatus FileUploadStatus
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
                return FileReachedSynapse ? ResubmissionTaskListStatus.Completed : ResubmissionTaskListStatus.Pending;
            }

            if (!FileReachedSynapse && ApplicationStatus is
                    ApplicationStatusType.FileUploaded or
                    ApplicationStatusType.SubmittedAndHasRecentFileUpload)
            {
                return ResubmissionTaskListStatus.Pending;
            }

            return ResubmissionTaskListStatus.NotStarted;
        }
    }

    public ResubmissionTaskListStatus PaymentViewStatus
    {
        get
        {
            if (FileUploadStatus == ResubmissionTaskListStatus.NotStarted || FileUploadStatus == ResubmissionTaskListStatus.Pending)
            {
                return ResubmissionTaskListStatus.CanNotStartYet;
            }

            if (FileUploadStatus is ResubmissionTaskListStatus.Completed && !ResubmissionFeePaid)
            {
                return ResubmissionTaskListStatus.NotStarted;
            }

            if (FileUploadStatus == ResubmissionTaskListStatus.Completed && ResubmissionFeePaid)
            {
                return ResubmissionTaskListStatus.Completed;
            }

            return ResubmissionTaskListStatus.NotStarted;
        }
    }

    public ResubmissionTaskListStatus AdditionalDetailsStatus
    {
        get
        {
            if (PaymentViewStatus is ResubmissionTaskListStatus.Completed && ResubmissionApplicationSubmitted)
            {
                return ResubmissionTaskListStatus.Completed;
            }

            if (PaymentViewStatus is ResubmissionTaskListStatus.NotStarted or ResubmissionTaskListStatus.CanNotStartYet)
            {
                return ResubmissionTaskListStatus.CanNotStartYet;
            }

            if (PaymentViewStatus is ResubmissionTaskListStatus.Completed && !ResubmissionApplicationSubmitted)
            {
                return ResubmissionTaskListStatus.NotStarted;
            }

            return ResubmissionTaskListStatus.NotStarted;
        }
    }

    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

    public bool? IsResubmitted { get; set; }

    public bool? IsResubmissionFeeViewed { get; set; }

    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public string? ResubmissionReferenceNumber { get; set; } = string.Empty;

    public LastSubmittedFileDetails LastSubmittedFile { get; set; } = new LastSubmittedFileDetails();

    public string? ResubmissionFeePaymentMethod { get; set; }

    public bool ResubmissionFeePaid => ResubmissionFeePaymentMethod is "PayByPhone" or "PayOnline" or "PayByBankTransfer";

    public DateTime? ResubmissionApplicationSubmittedDate { get; set; }

    public string? ResubmissionApplicationSubmittedComment { get; set; }

    public bool ResubmissionApplicationSubmitted => ResubmissionApplicationSubmittedDate is not null;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public bool FileReachedSynapse { get; set; }

    public bool IsResubmissionInProgress => ((FileUploadStatus != ResubmissionTaskListStatus.NotStarted && FileUploadStatus != ResubmissionTaskListStatus.CanNotStartYet) &&
                                            AdditionalDetailsStatus != ResubmissionTaskListStatus.Completed);

    public bool IsResubmissionComplete => (AdditionalDetailsStatus == ResubmissionTaskListStatus.Completed);

    public Organisation Organisation { get; set; } = new Organisation();
}