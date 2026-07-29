namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Extensions;
using NUnit.Framework;

/// <summary>
/// SUB-332: PomFileUploadDateTime comes from the latest antivirus check event, written for every upload
/// regardless of outcome, whereas LastUploadedValidFile falls back to an older file when the latest one
/// does not validate. A gap between them is the user's failed retry.
/// </summary>
[TestFixture]
public class PomSubmissionExtensionsTests
{
    [Test]
    public void HasNewerUnprocessedUploadThanValidFile_ShouldBeTrue_WhenLatestUploadNeverBecameTheValidFile()
    {
        var submission = new PomSubmission
        {
            PomFileName = "retry.csv",
            PomFileUploadDateTime = new DateTime(2026, 7, 28, 10, 2, 0, DateTimeKind.Utc),
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = "original.csv",
                FileUploadDateTime = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        submission.HasNewerUnprocessedUploadThanValidFile().Should().BeTrue();
    }

    [Test]
    public void HasNewerUnprocessedUploadThanValidFile_ShouldBeFalse_WhenLatestUploadIsTheValidFile()
    {
        var uploadedAt = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc);

        var submission = new PomSubmission
        {
            PomFileName = "original.csv",
            PomFileUploadDateTime = uploadedAt,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = "original.csv",
                FileUploadDateTime = uploadedAt
            }
        };

        submission.HasNewerUnprocessedUploadThanValidFile().Should().BeFalse();
    }

    [Test]
    public void HasNewerUnprocessedUploadThanValidFile_ShouldBeFalse_WhenThereIsNoValidFile()
    {
        var submission = new PomSubmission
        {
            PomFileName = "only-attempt.csv",
            PomFileUploadDateTime = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc),
            LastUploadedValidFile = null
        };

        submission.HasNewerUnprocessedUploadThanValidFile().Should().BeFalse();
    }

    [Test]
    public void HasNewerUnprocessedUploadThanValidFile_ShouldBeFalse_WhenUploadDateTimeIsUnknown()
    {
        var submission = new PomSubmission
        {
            PomFileUploadDateTime = null,
            LastUploadedValidFile = new UploadedFileInformation
            {
                FileId = Guid.NewGuid(),
                FileName = "original.csv",
                FileUploadDateTime = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        submission.HasNewerUnprocessedUploadThanValidFile().Should().BeFalse();
    }

    [Test]
    public void HasNewerUnprocessedUploadThanValidFile_ShouldBeFalse_WhenSubmissionIsNull()
    {
        PomSubmission submission = null;

        submission.HasNewerUnprocessedUploadThanValidFile().Should().BeFalse();
    }
}
