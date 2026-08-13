using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.Sessions;

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

    public InProgressSubmissionPeriodStatus? ApplicationInProgressSubmissionPeriodStatus
    {
        get
        {
            // SUB-345: both messages below tell the user what is outstanding on work they have already done,
            // so neither can be shown for a cycle that has merely been opened. The cycle survives a regulator
            // decision by design - the reference number keeps its identity - and FileReachedSynapse refers to
            // whichever file last reached Synapse, which for a ruled-on cycle is the file that was ruled on.
            // Between them that was enough to head an untouched cycle's tile with "you've viewed your fee, you
            // now need to submit to the regulator" while the task list showed every step unstarted.
            //
            // Neither legitimate state is lost: in both the file has been submitted, so ApplicationStatus is
            // SubmittedToRegulator and the cycle reads as started.
            if (!IsResubmissionStarted)
            {
                return null;
            }

            if ((!IsResubmissionFeeViewed.HasValue || !IsResubmissionFeeViewed.Value)
                && FileReachedSynapse && !ResubmissionApplicationSubmitted)
            {
                return InProgressSubmissionPeriodStatus.InProgress_Resubmission_FileInSynapse_FeesNotViewed_NotSubmitted;
            }

            if (IsResubmissionFeeViewed.HasValue && IsResubmissionFeeViewed.Value
                && !ResubmissionApplicationSubmitted && FileReachedSynapse)
            {
                return InProgressSubmissionPeriodStatus.InProgress_Resubmission_FeesViewed_NotSubmitted;
            }

            return null;
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

    /// <summary>
    /// SUB-345: the most recent resubmission cycle the regulator has ruled on, or null if there is none.
    /// </summary>
    /// <remarks>
    /// Every other property here describes the cycle that is open now, and all of them stop describing a cycle
    /// at the decision that closed it - correctly, because none of that state belongs to whatever the user does
    /// next. That leaves nothing to tell a completed resubmission from one never started, which is what had an
    /// accepted resubmission's tile offering to begin the journey again.
    /// </remarks>
    public CompletedResubmissionDetails? LastCompletedResubmission { get; set; }

    /// <summary>
    /// SUB-345: true when the regulator has ruled on a resubmission this organisation completed for the period.
    /// </summary>
    public bool HasCompletedResubmission => LastCompletedResubmission is not null;

    public bool FileReachedSynapse { get; set; }

    // SUB-332: derived from the authoritative cycle fields rather than from FileUploadStatus, which is
    // itself derived from upload validity. An upload that failed validation used to collapse this to false
    // mid-cycle, which removed the only route back into the resubmission journey.
    public bool IsResubmissionInProgress => !string.IsNullOrEmpty(ApplicationReferenceNumber) && !ResubmissionApplicationSubmitted;

    public bool IsResubmissionComplete => (AdditionalDetailsStatus == ResubmissionTaskListStatus.Completed);

    /// <summary>
    /// True when this resubmission cycle has actually been started, as opposed to merely being open.
    /// </summary>
    /// <remarks>
    /// SUB-345: IsResubmissionInProgress is satisfied by ApplicationReferenceNumber alone, and that number
    /// survives a regulator decision so the cycle keeps its identity. On its own it therefore reads as
    /// "in progress" for a cycle the user has not touched, which headed an untouched task list with
    /// "Continue your packaging data resubmission" while every step below it read Not started.
    /// The landing pages already pair the flag with this ApplicationStatus check; this is that pairing,
    /// named once rather than repeated inline.
    /// </remarks>
    public bool IsResubmissionStarted => IsResubmissionInProgress && ApplicationStatus != ApplicationStatusType.NotStarted;

    public Organisation Organisation { get; set; } = new Organisation();
    
    public bool HasSubmissionSyncCompleted { get; set; }
}