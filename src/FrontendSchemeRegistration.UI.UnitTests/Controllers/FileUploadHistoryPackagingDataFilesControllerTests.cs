using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    public class FileUploadHistoryPackagingDataFilesControllerTests
    {
        private Mock<ISubmissionService> _submissionServiceMock;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
        private Mock<HttpContext> _httpContextMock;
        private Mock<ClaimsPrincipal> _userMock;
        private UserData _userData;
        private Guid _organisationId = Guid.NewGuid();
        private Guid _userId = Guid.NewGuid();
        private FileUploadHistoryPackagingDataFilesController _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _httpContextMock = new Mock<HttpContext>();
            _userMock = new Mock<ClaimsPrincipal>();
            _submissionServiceMock = new Mock<ISubmissionService>();
            _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            _systemUnderTest = new FileUploadHistoryPackagingDataFilesController(_submissionServiceMock.Object, _sessionMock.Object);

            _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;

            _userData = new UserData
            {
                Email = "test@test.com",
                Organisations = new List<Organisation> { new() { Id = _organisationId } }
            };

            _userMock.Setup(x => x.Claims).Returns(new List<Claim>
            {
                new(ClaimTypes.UserData, JsonConvert.SerializeObject(_userData)),
                new(ClaimConstants.ObjectId, _userId.ToString())
            });

            _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        }

        [Test]
        public async Task Get_ReturnsPackagingDataFiles_WhenDataExists()
        {
            // Arrange
            var year = 2020;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = year },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = year },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = year - 1 }
            };

            var submissionHistoryDictionary = new Dictionary<Guid, List<SubmissionHistory>>
            {
                {
                    submissionIds[0].SubmissionId,
                    new List<SubmissionHistory>
                    {
                        new SubmissionHistory
                        {
                            SubmissionId = submissionIds[0].SubmissionId,
                            FileName = "test.csv",
                            UserName = "John Doe",
                            SubmissionDate = new DateTime(submissionIds[0].Year, 8, 20),
                            Status = "Accepted",
                            DateofLatestStatusChange = new DateTime(submissionIds[0].Year, 9, 1)
                        },
                        new SubmissionHistory
                        {
                            SubmissionId = submissionIds[0].SubmissionId,
                            FileName = "test.csv",
                            UserName = "John Doe",
                            SubmissionDate = new DateTime(submissionIds[0].Year, 8, 1),
                            Status = "Rejected",
                            DateofLatestStatusChange = new DateTime(submissionIds[0].Year, 8, 10)
                        }
                    }
                },
                {
                    submissionIds[1].SubmissionId,
                    new List<SubmissionHistory>
                    {
                        new SubmissionHistory
                        {
                            SubmissionId = submissionIds[1].SubmissionId,
                            FileName = "test.csv",
                            UserName = "John Doe",
                            SubmissionDate = new DateTime(submissionIds[1].Year, 8, 20),
                            Status = "Accepted",
                            DateofLatestStatusChange = new DateTime(submissionIds[1].Year, 9, 1)
                        }
                    }
                },
                {
                    submissionIds[2].SubmissionId,
                    new List<SubmissionHistory>
                    {
                        new SubmissionHistory
                        {
                            SubmissionId = submissionIds[1].SubmissionId,
                            FileName = "test.csv",
                            UserName = "John Doe",
                            SubmissionDate = new DateTime(submissionIds[2].Year, 8, 20),
                            Status = "Accepted",
                            DateofLatestStatusChange = new DateTime(submissionIds[2].Year, 9, 1)
                        }
                    }
                }
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, year))
                .ReturnsAsync((Guid organisationId, SubmissionType type, Guid? complianceSchemaId, int yearFilter) =>
                {
                    return submissionIds.Where(s => s.Year == yearFilter).ToList();
                });

            _submissionServiceMock.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .ReturnsAsync((Guid submissionId, DateTime lastSyncTime) =>
                {
                    return submissionHistoryDictionary.TryGetValue(submissionId, out var submissionHistory)
                        ? submissionHistory
                        : new List<SubmissionHistory>();
                });

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get(year) as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadHistoryPackagingDataFiles");
            result.Model.Should().BeEquivalentTo(new FileUploadHistoryPackagingDataFilesViewModel
            {
                Year = year,
                SubmissionPeriods = new List<FileUploadSubmissionHistoryPeriodViewModel>
                {
                    new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionIds[0].SubmissionPeriod,
                        SubmissionHistory = submissionHistoryDictionary[submissionIds[0].SubmissionId]
                    },
                    new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionIds[1].SubmissionPeriod,
                        SubmissionHistory = submissionHistoryDictionary[submissionIds[1].SubmissionId]
                    }
                }
            });
        }

        [Test]
        public async Task Get_ReturnsRedirect_WhenSubmissionDataDoesNotExist()
        {
            // Arrange
            var year = 2020;

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, year))
                .ReturnsAsync((Guid organisationId, SubmissionType type, Guid? complianceSchemaId, int yearFilter) => new List<SubmissionPeriodId>());

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get(year) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(nameof(FileUploadNoSubmissionHistoryController.Get));
            result.ControllerName.Should().Be(nameof(FileUploadNoSubmissionHistoryController).RemoveControllerFromName());
        }

        [Test]
        public async Task Get_ReturnsRedirect_WhenSubmissionHistoryDataDoesNotExist()
        {
            // Arrange
            var year = 2020;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = year },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = year }
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, year))
                .ReturnsAsync((Guid organisationId, SubmissionType type, Guid? complianceSchemaId, int yearFilter) =>
                {
                    return submissionIds.Where(s => s.Year == yearFilter).ToList();
                });

            _submissionServiceMock.Setup(x => x.GetSubmissionHistoryAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .ReturnsAsync((Guid submissionId, DateTime lastSyncTime) => new List<SubmissionHistory>());

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get(year) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(nameof(FileUploadNoSubmissionHistoryController.Get));
            result.ControllerName.Should().Be(nameof(FileUploadNoSubmissionHistoryController).RemoveControllerFromName());
        }

        [Test]
        public async Task Get_ReturnsRedirect_WhenYearParameterIsTooSmall()
        {
            // Arrange
            var year = 1970;

            // Act
            var result = await _systemUnderTest.Get(year) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(nameof(FileUploadNoSubmissionHistoryController.Get));
            result.ControllerName.Should().Be(nameof(FileUploadNoSubmissionHistoryController).RemoveControllerFromName());
        }

        [Test]
        public async Task Get_ReturnsRedirect_WhenYearParameterIsTooBig()
        {
            // Arrange
            var year = DateTime.Now.AddYears(2).Year;

            // Act
            var result = await _systemUnderTest.Get(year) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(nameof(FileUploadNoSubmissionHistoryController.Get));
            result.ControllerName.Should().Be(nameof(FileUploadNoSubmissionHistoryController).RemoveControllerFromName());
        }
    }
}
