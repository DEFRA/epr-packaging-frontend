namespace FrontendSchemeRegistration.UI.UnitTests.Sessions;

using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Sessions;
using NUnit.Framework;

/// <summary>
/// SUB-332: the cycle-state flags previously chained off FileUploadStatus, which is derived from upload
/// validity. A retry that failed validation collapsed IsResubmissionInProgress to false mid-cycle and
/// removed the only route back into the resubmission journey.
/// </summary>
[TestFixture]
public class PackagingResubmissionApplicationSessionTests
{
    [Test]
    public void IsResubmissionInProgress_ShouldBeFalse_WhenNoCycleHasBeenStarted()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = string.Empty,
            ApplicationStatus = ApplicationStatusType.NotStarted
        };

        session.IsResubmissionInProgress.Should().BeFalse();
        session.IsResubmissionComplete.Should().BeFalse();
    }

    [Test]
    public void IsResubmissionInProgress_ShouldBeTrue_WhenCycleIsOpen()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            ResubmissionApplicationSubmittedDate = null
        };

        session.IsResubmissionInProgress.Should().BeTrue();
        session.IsResubmissionComplete.Should().BeFalse();
    }

    // The SUB-332 incident: a file was submitted, a later upload failed validation, and the declaration was
    // never reached. FileReachedSynapse is false so FileUploadStatus is not Completed - the cycle must
    // still read as in progress.
    [Test]
    public void IsResubmissionInProgress_ShouldBeTrue_WhenCycleIsOpenAndLatestUploadWasNotValid()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = false,
            ResubmissionApplicationSubmittedDate = null
        };

        session.FileUploadStatus.Should().Be(ResubmissionTaskListStatus.Pending);
        session.IsResubmissionInProgress.Should().BeTrue();
    }

    [Test]
    public void IsResubmissionInProgress_ShouldBeFalse_OnceTheApplicationHasBeenSubmitted()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            ResubmissionApplicationSubmittedDate = DateTime.Now
        };

        session.IsResubmissionInProgress.Should().BeFalse();
    }

    [Test]
    public void IsResubmissionComplete_ShouldBeTrue_WhenDeclaredAndFeePaidAndFileSynced()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = true,
            ResubmissionFeePaymentMethod = "PayOnline",
            ResubmissionApplicationSubmittedDate = DateTime.Now
        };

        session.IsResubmissionComplete.Should().BeTrue();
        session.IsResubmissionInProgress.Should().BeFalse();
    }

    // IsResubmissionComplete deliberately keeps its fee and Synapse dependencies, so between declaring and
    // the sync completing both flags read false. FileUploadSubLandingController and FileUploadSubLanding
    // rely on ResubmissionApplicationSubmitted to keep an action on the tile through that window.
    [Test]
    public void BothCycleFlags_ShouldBeFalse_BetweenDeclarationAndSynapseSync()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = false,
            ResubmissionFeePaymentMethod = "PayOnline",
            ResubmissionApplicationSubmittedDate = DateTime.Now
        };

        session.IsResubmissionInProgress.Should().BeFalse();
        session.IsResubmissionComplete.Should().BeFalse();
        session.ResubmissionApplicationSubmitted.Should().BeTrue();
    }

    // SUB-332: the reported task-list state - the file is uploaded, synced and paid for, but nothing has been
    // declared for this cycle, so only the last step is outstanding. Reporting a superseded declaration
    // against the cycle marked that step Completed and pointed its link at the confirmation page.
    [Test]
    public void TaskListStatuses_ShouldLeaveOnlyTheDeclarationOutstanding_WhenTheFeeIsPaidButNothingHasBeenDeclared()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = true,
            HasSubmissionSyncCompleted = true,
            ResubmissionFeePaymentMethod = "PayByPhone",
            ResubmissionApplicationSubmittedDate = null
        };

        session.FileUploadStatus.Should().Be(ResubmissionTaskListStatus.Completed);
        session.PaymentViewStatus.Should().Be(ResubmissionTaskListStatus.Completed);
        session.AdditionalDetailsStatus.Should().Be(ResubmissionTaskListStatus.NotStarted);
        session.ResubmissionApplicationSubmitted.Should().BeFalse();
        session.IsResubmissionInProgress.Should().BeTrue();
    }

    // SUB-332: an upload that never produced a valid file reports NotStarted, which is what keeps the upload
    // step startable so the user can replace it. The reference number, not the status, is what keeps the
    // cycle in progress.
    [Test]
    public void FileUploadStatus_ShouldBeNotStarted_WithTheCycleStillInProgress_WhenTheLatestUploadDidNotValidate()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.NotStarted,
            FileReachedSynapse = true,
            ResubmissionApplicationSubmittedDate = null
        };

        session.FileUploadStatus.Should().Be(ResubmissionTaskListStatus.NotStarted);
        session.PaymentViewStatus.Should().Be(ResubmissionTaskListStatus.CanNotStartYet);
        session.AdditionalDetailsStatus.Should().Be(ResubmissionTaskListStatus.CanNotStartYet);
        session.IsResubmissionInProgress.Should().BeTrue();
    }

    // SUB-345: the state the user lands in after the regulator accepts a declared cycle. The reference number
    // survives so the cycle keeps its identity, but nothing has been done in the new cycle: every task-list
    // step is unstarted, so the heading must not offer to "continue" it.
    [Test]
    public void IsResubmissionStarted_ShouldBeFalse_WhenTheCycleIsOpenButUntouched()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.NotStarted,
            ResubmissionApplicationSubmittedDate = null
        };

        session.IsResubmissionInProgress.Should().BeTrue();
        session.IsResubmissionStarted.Should().BeFalse();
        session.FileUploadStatus.Should().Be(ResubmissionTaskListStatus.NotStarted);
    }

    // The upload is registered but not yet submitted or synced, so FileUploadStatus is only Pending. The
    // heading keys off ApplicationStatus rather than FileUploadStatus so that this still counts as started -
    // the user has uploaded something and does have work to continue.
    [Test]
    public void IsResubmissionStarted_ShouldBeTrue_OnceAFileHasBeenUploadedIntoTheCycle()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            FileReachedSynapse = false,
            ResubmissionApplicationSubmittedDate = null
        };

        session.FileUploadStatus.Should().Be(ResubmissionTaskListStatus.Pending);
        session.IsResubmissionStarted.Should().BeTrue();
    }

    // Started is a narrowing of in progress, never a widening: once the cycle is declared it is no longer
    // either, and the heading falls back to the organisation-named one.
    [Test]
    public void IsResubmissionStarted_ShouldBeFalse_OnceTheApplicationHasBeenSubmitted()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            ResubmissionApplicationSubmittedDate = DateTime.Now
        };

        session.IsResubmissionInProgress.Should().BeFalse();
        session.IsResubmissionStarted.Should().BeFalse();
    }

    // SUB-345: the sub-landing tile message for the state the user lands in after the regulator rules on a
    // declared cycle. The fee flags belong to the closed cycle and FileReachedSynapse still refers to the file
    // that was ruled on, so both branches below would otherwise fire for a cycle nothing has been done in.
    [Test]
    public void ApplicationInProgressSubmissionPeriodStatus_ShouldBeNull_WhenTheCycleIsOpenButUntouched()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.NotStarted,
            FileReachedSynapse = true,
            IsResubmissionFeeViewed = true,
            ResubmissionApplicationSubmittedDate = null
        };

        session.IsResubmissionStarted.Should().BeFalse();
        session.ApplicationInProgressSubmissionPeriodStatus.Should().BeNull();
    }

    // The same untouched cycle with the API's fee reset applied. Without the started check this swaps one wrong
    // message for another - "your file is in Synapse, you haven't viewed your fee" - rather than falling silent.
    [Test]
    public void ApplicationInProgressSubmissionPeriodStatus_ShouldBeNull_WhenTheCycleIsUntouchedAndTheFeeWasNeverViewed()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.NotStarted,
            FileReachedSynapse = true,
            IsResubmissionFeeViewed = null,
            ResubmissionApplicationSubmittedDate = null
        };

        session.ApplicationInProgressSubmissionPeriodStatus.Should().BeNull();
    }

    // The legitimate state the fee-viewed message exists for: the file is submitted and synced, the fee has been
    // viewed, and only the declaration is outstanding.
    [Test]
    public void ApplicationInProgressSubmissionPeriodStatus_ShouldReportTheDeclarationOutstanding_WhenTheFeeHasBeenViewedInAStartedCycle()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = true,
            IsResubmissionFeeViewed = true,
            ResubmissionApplicationSubmittedDate = null
        };

        session.IsResubmissionStarted.Should().BeTrue();
        session.ApplicationInProgressSubmissionPeriodStatus.Should()
            .Be(InProgressSubmissionPeriodStatus.InProgress_Resubmission_FeesViewed_NotSubmitted);
    }

    // The other legitimate state: the same started cycle before the fee has been looked at.
    [Test]
    public void ApplicationInProgressSubmissionPeriodStatus_ShouldReportTheFeeOutstanding_WhenItHasNotBeenViewedInAStartedCycle()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = true,
            IsResubmissionFeeViewed = null,
            ResubmissionApplicationSubmittedDate = null
        };

        session.IsResubmissionStarted.Should().BeTrue();
        session.ApplicationInProgressSubmissionPeriodStatus.Should()
            .Be(InProgressSubmissionPeriodStatus.InProgress_Resubmission_FileInSynapse_FeesNotViewed_NotSubmitted);
    }

    // Both messages describe outstanding work, so once the cycle is declared there is none to describe. This was
    // already the behaviour through the !ResubmissionApplicationSubmitted checks; the started guard keeps it.
    [Test]
    public void ApplicationInProgressSubmissionPeriodStatus_ShouldBeNull_OnceTheApplicationHasBeenSubmitted()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = true,
            IsResubmissionFeeViewed = true,
            ResubmissionApplicationSubmittedDate = DateTime.Now
        };

        session.ApplicationInProgressSubmissionPeriodStatus.Should().BeNull();
    }

    [Test]
    public void IsResubmissionComplete_ShouldBeFalse_WhenDeclaredButFeeNotPaid()
    {
        var session = new PackagingResubmissionApplicationSession
        {
            ApplicationReferenceNumber = "PEPR12345S01",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            FileReachedSynapse = true,
            ResubmissionFeePaymentMethod = null,
            ResubmissionApplicationSubmittedDate = DateTime.Now
        };

        session.IsResubmissionComplete.Should().BeFalse();
    }
}
