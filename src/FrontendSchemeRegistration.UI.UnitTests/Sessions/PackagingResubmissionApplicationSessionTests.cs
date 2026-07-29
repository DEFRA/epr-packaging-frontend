namespace FrontendSchemeRegistration.UI.UnitTests.Sessions;

using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
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
