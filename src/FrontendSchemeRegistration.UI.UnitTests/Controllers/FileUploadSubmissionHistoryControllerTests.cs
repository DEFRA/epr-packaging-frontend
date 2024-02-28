using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    public class FileUploadSubmissionHistoryControllerTests
    {
        private Mock<ISubmissionService> _submissionServiceMock;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
        private Mock<HttpContext> _httpContextMock;
        private Mock<ClaimsPrincipal> _userMock;
        private UserData _userData;
        private Guid _organisationId = Guid.NewGuid();
        private Guid _userId = Guid.NewGuid();
        private FileUploadSubmissionHistoryController _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);

            _httpContextMock = new Mock<HttpContext>();
            _userMock = new Mock<ClaimsPrincipal>();
            _submissionServiceMock = new Mock<ISubmissionService>();
            _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            _systemUnderTest = new FileUploadSubmissionHistoryController(_submissionServiceMock.Object, _sessionMock.Object);

            _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
            _systemUnderTest.Url = urlHelperMock.Object;

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
        public async Task Get_ReturnsSubmissionPeriods_WhenCurentYearDataExists()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 2 }
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
                }
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync((Guid organisationId, SubmissionType type, Guid? complianceSchemaId, int year) =>
                {
                    return submissionIds.Where(s => year <= s.Year).ToList();
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
            var result = await _systemUnderTest.Get() as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadSubmissionHistory");
            result.ViewData.Keys.Should().HaveCount(1);
            result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
            result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
            result.Model.Should().BeEquivalentTo(new FileUploadSubmissionHistoryViewModel
            {
                PreviousSubmissionHistoryExists = true,
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
        public async Task Get_ReturnsEmptySubmissionPeriods_WhenCurrentYearDataNotExist()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = currentYear - 1 }
            };

            var submissionHistoryDictionary = new Dictionary<Guid, List<SubmissionHistory>>();

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(submissionIds);

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
            var result = await _systemUnderTest.Get() as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadSubmissionHistory");
            result.ViewData.Keys.Should().HaveCount(1);
            result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
            result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
            result.Model.Should().BeEquivalentTo(new FileUploadSubmissionHistoryViewModel
            {
                PreviousSubmissionHistoryExists = true,
                SubmissionPeriods = new List<FileUploadSubmissionHistoryPeriodViewModel>()
            });
        }

        [Test]
        public async Task Get_ReturnsBooleanWhichTellsThatThereIsNoPreviousHistoryDataToShow_WhenPreviousHistoryDataDoesNotExist()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = currentYear }
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
                }
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(submissionIds);

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
            var result = await _systemUnderTest.Get() as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadSubmissionHistory");
            result.ViewData.Keys.Should().HaveCount(1);
            result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
            result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubLanding}");
            result.Model.Should().BeEquivalentTo(new FileUploadSubmissionHistoryViewModel
            {
                PreviousSubmissionHistoryExists = false,
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
    }
}
