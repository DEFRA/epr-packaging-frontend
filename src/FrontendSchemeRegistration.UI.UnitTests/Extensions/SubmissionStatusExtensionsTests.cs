namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.Extensions.Time.Testing;

[TestFixture]
public class SubmissionStatusExtensionsTests
{
    private FakeTimeProvider _timeProvider;

    [SetUp]
    public void Setup()
    {
        // Set a fixed date for testing: 2025-01-15
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
    }

    [Test]
    [TestCase(null, "Jan 01 2024", null, false, SubmissionPeriodStatus.NotStarted)]
    [TestCase(null, "July 10 2026", null, false, SubmissionPeriodStatus.CannotStartYet)]
    public void SubmissionStatusExtensions_GetSubmissionStatus_WhenSumission_IsNull_ShouldReturn_SubmissionPeriodStatusCorrectly(
        RegistrationSubmission submission, string dataPeriod,
        RegistrationDecision decision,
        bool showRegistrationDecision, SubmissionPeriodStatus expectedResult)
    {
        var submissionPeriod = new SubmissionPeriod { ActiveFrom = DateTime.Parse(dataPeriod) };
        var result = submission.GetSubmissionStatus(submissionPeriod, decision, showRegistrationDecision, _timeProvider);
        result.Should().Be(expectedResult);
    }

    [Test]
    [TestCase(false, "Jan 01 2024", null, false, true, SubmissionPeriodStatus.FileUploaded)]
    [TestCase(false, "July 10 2023", null, false, false, SubmissionPeriodStatus.NotStarted)]
    [TestCase(true, "July 10 2023", null, false, false, SubmissionPeriodStatus.SubmittedToRegulator)]
    [TestCase(true, "July 10 2023", null, false, true, SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload)]
    [TestCase(true, "July 10 2023", null, true, false, SubmissionPeriodStatus.SubmittedToRegulator)]
    [TestCase(true, "July 10 2023", null, true, true, SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload)]
    [TestCase(true, "July 10 2023", "Accepted", true, true, SubmissionPeriodStatus.AcceptedByRegulator)]
    [TestCase(true, "July 10 2023", "Approved", true, true, SubmissionPeriodStatus.AcceptedByRegulator)]
    [TestCase(true, "July 10 2023", "Rejected", true, true, SubmissionPeriodStatus.RejectedByRegulator)]
    public void SubmissionStatusExtensions_GetSubmissionStatus_ShouldReturn_SubmissionPeriodStatusCorrectly(
        bool isSubmitted, string dataPeriod, string decision,
        bool showRegistrationDecision, bool isLastUploadedValidFiles, SubmissionPeriodStatus expectedResult)
    {
        var submission = new RegistrationSubmission { IsSubmitted = isSubmitted };
        var submissionPeriod = new SubmissionPeriod { ActiveFrom = DateTime.Parse(dataPeriod) };
        var registrationDecision = new RegistrationDecision { Decision = decision };
        if (isLastUploadedValidFiles)
        {
            submission.LastUploadedValidFiles = new UploadedRegistrationFilesInformation { CompanyDetailsUploadDatetime = DateTime.Parse("July 10 2026") };
            submission.LastSubmittedFiles = new SubmittedRegistrationFilesInformation { SubmittedDateTime = DateTime.Parse("July 10 2024") };
        }

        var result = submission.GetSubmissionStatus(submissionPeriod, registrationDecision, showRegistrationDecision, _timeProvider);
        result.Should().Be(expectedResult);
    }
}
