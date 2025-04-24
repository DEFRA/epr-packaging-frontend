using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;

namespace FrontendSchemeRegistration.UI.UnitTests.Extensions
{
    public class PackagingResubmissionApplicationDetailsExtensionTests
    {
        [Test]
        public void ToPackagingResubmissionApplicationSession_ShouldReturn_ListOf_PackagingResubmissionApplicationSession_WhenValidInput()
        {
            // Arrange
            var organisation = new Organisation { Id = Guid.NewGuid() };

            var packagingResubmissionApplicationDetails = new List<PackagingResubmissionApplicationDetails>
            {
                new PackagingResubmissionApplicationDetails
                {
                    ApplicationReferenceNumber = "abc",
                    SubmissionId = Guid.NewGuid(),
                },
                new PackagingResubmissionApplicationDetails
                {
                    ApplicationReferenceNumber = "def",
                    SubmissionId = Guid.NewGuid()
                },
            };

            var expecatedCollection = new List<PackagingResubmissionApplicationSession>
            {
                new PackagingResubmissionApplicationSession
                {
                    ApplicationReferenceNumber = "abc",
                    SubmissionId = packagingResubmissionApplicationDetails[0].SubmissionId
                },
                new PackagingResubmissionApplicationSession
                {
                    ApplicationReferenceNumber = "def",
                    SubmissionId = packagingResubmissionApplicationDetails[1].SubmissionId
                }
            };

            // Act
            var result = packagingResubmissionApplicationDetails.ToPackagingResubmissionApplicationSessionList(organisation);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<PackagingResubmissionApplicationSession>>();
            result.Count.Should().Be(2);
            result.First().SubmissionId.Should().Be(packagingResubmissionApplicationDetails[0].SubmissionId);
        }

        [Test]
        public void ToPackagingResubmissionApplicationSession_ShouldReturn_PackagingResubmissionApplicationSession_WhenValidInput()
        {
            // Arrange
            var packagingResubmissionApplicationDetails = new PackagingResubmissionApplicationDetails
            {
                ApplicationReferenceNumber = "abc",
                SubmissionId = Guid.NewGuid(),
            };

            var organisation = new Organisation { Id = Guid.NewGuid() };

            var expected = new PackagingResubmissionApplicationSession
            {
                ApplicationReferenceNumber = "abc",
                SubmissionId = packagingResubmissionApplicationDetails.SubmissionId
            };

            // Act
            var result = packagingResubmissionApplicationDetails.ToPackagingResubmissionApplicationSession(organisation);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<PackagingResubmissionApplicationSession>();
            result.SubmissionId.Should().Be(packagingResubmissionApplicationDetails.SubmissionId);
        }

        [Test]
        public void ToResubmissionTaskListViewModel_ShouldReturn_ResubmissionTaskListViewModel_WhenValidInput()
        {
            // Arrange
            var packagingResubmissionApplicationDetails = new List<PackagingResubmissionApplicationDetails>
            {
                new PackagingResubmissionApplicationDetails
                {
                    ApplicationReferenceNumber = "abc",
                    SubmissionId = Guid.NewGuid(),
                    ApplicationStatus = ApplicationStatusType.FileUploaded
                }
            };

            var organisation = new Organisation { Id = Guid.NewGuid() };

            // Act
            var result = packagingResubmissionApplicationDetails.ToResubmissionTaskListViewModel(organisation);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ResubmissionTaskListViewModel>();
            result.AppReferenceNumber.Should().Be(packagingResubmissionApplicationDetails.FirstOrDefault().ApplicationReferenceNumber);
            result.ApplicationStatus.Should().Be(packagingResubmissionApplicationDetails.FirstOrDefault().ApplicationStatus);
        }
    }
}
