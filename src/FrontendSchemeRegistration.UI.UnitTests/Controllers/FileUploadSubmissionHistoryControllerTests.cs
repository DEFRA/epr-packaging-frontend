﻿using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Extensions;
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
        [SetUICulture(Language.English)]
        public async Task Get_ReturnsSubmissionPeriods_WhenDataExists()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionPeriod = "July to December 2024",
                    DatePeriodStartMonth = "July",
                    DatePeriodEndMonth = "December",
                    Year = currentYear
                },
                new SubmissionPeriodId
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionPeriod = "January to June 2024",
                    DatePeriodStartMonth = "January",
                    DatePeriodEndMonth = "June",
                    Year = currentYear
                },
                new SubmissionPeriodId
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionPeriod = "July to December 2024",
                    DatePeriodStartMonth = "July",
                    DatePeriodEndMonth = "December",
                    Year = currentYear - 1
                }
            };

            var submissionDate = new DateTime(submissionIds[0].Year, 2, 20, 8, 0, 0, DateTimeKind.Utc);
            var dateofLatestStatusChange = new DateTime(submissionIds[0].Year, 3, 1, 5, 0, 0, DateTimeKind.Utc);
            var submissionDate1 = new DateTime(submissionIds[0].Year, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var dateofLatestStatusChange1 = new DateTime(submissionIds[0].Year, 8, 10, 6, 0, 0, DateTimeKind.Utc);

            var expectedLocalSubmissionDate = new DateTime(submissionIds[0].Year, 2, 20, 8, 0, 0, DateTimeKind.Local);
            var expectedLocalDateofLatestStatusChange = new DateTime(submissionIds[0].Year, 3, 1, 5, 0, 0, DateTimeKind.Local);
            var expectedLocalSubmissionDate1 = new DateTime(submissionIds[0].Year, 8, 1, 1, 0, 0, DateTimeKind.Local);
            var expectedLocalDateofLatestStatusChange1 = new DateTime(submissionIds[0].Year, 8, 10, 7, 0, 0, DateTimeKind.Local);

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
                            SubmissionDate = submissionDate,
                            Status = "Accepted",
                            DateofLatestStatusChange = dateofLatestStatusChange
                        },
                        new SubmissionHistory
                        {
                            SubmissionId = submissionIds[0].SubmissionId,
                            FileName = "test.csv",
                            UserName = "John Doe",
                            SubmissionDate = submissionDate1,
                            Status = "Rejected",
                            DateofLatestStatusChange = dateofLatestStatusChange1
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
                            SubmissionDate = submissionDate,
                            Status = "Accepted",
                            DateofLatestStatusChange = dateofLatestStatusChange
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
                PreviousSubmissionHistoryExists = false,
                SubmissionPeriods = new List<FileUploadSubmissionHistoryPeriodViewModel>
                {
                    new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionIds[0].SubmissionPeriod,
                        DatePeriodStartMonth = submissionIds[0].DatePeriodStartMonth,
                        DatePeriodEndMonth = submissionIds[0].DatePeriodEndMonth,
                        DatePeriodYear = submissionIds[0].SubmissionPeriod.ToStartEndDate().Start.Year.ToString(),
                        SubmissionHistory = submissionHistoryDictionary[submissionIds[0].SubmissionId]
                    },
                    new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionIds[1].SubmissionPeriod,
                        DatePeriodStartMonth = submissionIds[1].DatePeriodStartMonth,
                        DatePeriodEndMonth = submissionIds[1].DatePeriodEndMonth,
                        DatePeriodYear = submissionIds[1].SubmissionPeriod.ToStartEndDate().Start.Year.ToString(),
                        SubmissionHistory = submissionHistoryDictionary[submissionIds[1].SubmissionId]
                    }
                }
            });

            var model = result.ViewData.Model as FileUploadSubmissionHistoryViewModel;

            model.SubmissionPeriods[0].SubmissionHistory[0].SubmissionDate.Should().Be(expectedLocalSubmissionDate);
            model.SubmissionPeriods[0].SubmissionHistory[0].DateofLatestStatusChange.Should().Be(expectedLocalDateofLatestStatusChange);
            model.SubmissionPeriods[0].SubmissionHistory[1].SubmissionDate.Should().Be(expectedLocalSubmissionDate1);
            model.SubmissionPeriods[0].SubmissionHistory[1].DateofLatestStatusChange.Should().Be(expectedLocalDateofLatestStatusChange1);
            model.SubmissionPeriods[1].SubmissionHistory[0].SubmissionDate.Should().Be(expectedLocalSubmissionDate);
            model.SubmissionPeriods[1].SubmissionHistory[0].DateofLatestStatusChange.Should().Be(expectedLocalDateofLatestStatusChange);
        }

        [Test]
        public async Task Get_ReturnsEmptySubmissionPeriods_WhenDataDoesNotExist()
        {
            // Arrange
            var submissionHistoryDictionary = new Dictionary<Guid, List<SubmissionHistory>>();

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(new List<SubmissionPeriodId>());

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
                new SubmissionPeriodId
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionPeriod = "July to December 2024",
                    DatePeriodStartMonth = "July",
                    DatePeriodEndMonth = "December",
                    Year = currentYear
                },
                new SubmissionPeriodId
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionPeriod = "January to June 2024",
                    DatePeriodStartMonth = "January",
                    DatePeriodEndMonth = "June",
                    Year = currentYear
                }
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
                        DatePeriodStartMonth = submissionIds[0].DatePeriodStartMonth,
                        DatePeriodEndMonth = submissionIds[0].DatePeriodEndMonth,
                        DatePeriodYear = submissionIds[0].SubmissionPeriod.ToStartEndDate().Start.Year.ToString(),
                        SubmissionHistory = submissionHistoryDictionary[submissionIds[0].SubmissionId]
                    },
                    new FileUploadSubmissionHistoryPeriodViewModel
                    {
                        SubmissionPeriod = submissionIds[1].SubmissionPeriod,
                        DatePeriodStartMonth = submissionIds[1].DatePeriodStartMonth,
                        DatePeriodEndMonth = submissionIds[1].DatePeriodEndMonth,
                        DatePeriodYear = submissionIds[1].SubmissionPeriod.ToStartEndDate().Start.Year.ToString(),
                        SubmissionHistory = submissionHistoryDictionary[submissionIds[1].SubmissionId]
                    }
                }
            });
        }
    }
}
