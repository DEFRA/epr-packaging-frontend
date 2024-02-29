using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.ControllerExtensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Identity.Web;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    public class FileUploadHistoryPreviousSubmissionsControllerTests
    {
        private Mock<ISubmissionService> _submissionServiceMock;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionMock;
        private Mock<HttpContext> _httpContextMock;
        private Mock<ClaimsPrincipal> _userMock;
        private Mock<IUrlHelper> _urlHelperMock;
        private UserData _userData;
        private Guid _organisationId = Guid.NewGuid();
        private Guid _userId = Guid.NewGuid();
        private FileUploadHistoryPreviousSubmissionsController _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
                .Returns(PagePaths.FileUploadHistoryPreviousSubmissions);
            _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);

            _httpContextMock = new Mock<HttpContext>();
            _userMock = new Mock<ClaimsPrincipal>();
            _submissionServiceMock = new Mock<ISubmissionService>();
            _sessionMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            _systemUnderTest = new FileUploadHistoryPreviousSubmissionsController(_submissionServiceMock.Object, _sessionMock.Object);

            _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
            _systemUnderTest.Url = _urlHelperMock.Object;

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
        public async Task Get_ReturnsSubmissionYears_WhenCalled()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 1 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = currentYear - 1 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 2 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "January to June", Year = currentYear - 2 }
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(submissionIds);

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get() as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadHistoryPreviousSubmissions");
            result.ViewData.Keys.Should().HaveCount(1);
            result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
            result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubmissionHistory}");
            result.Model.Should().BeEquivalentTo(new FileUploadHistoryPreviousSubmissionsViewModel
            {
                Years = new List<int> { currentYear - 1, currentYear - 2 },
                PagingDetail = new PagingDetail
                {
                    CurrentPage = 1,
                    PageSize = 5,
                    TotalItems = 2,
                    PagingLink = $"{PagePaths.FileUploadHistoryPreviousSubmissions}?page="
                }
            });
        }

        [Test]
        public async Task Get_ReturnsCorrectSubmissionYears_WhenSecondPageIsAccessed()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 1 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 2 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 3 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 4 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 5 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 6 },
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(submissionIds);

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get(2) as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadHistoryPreviousSubmissions");
            result.ViewData.Keys.Should().HaveCount(1);
            result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
            result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubmissionHistory}");
            result.Model.Should().BeEquivalentTo(new FileUploadHistoryPreviousSubmissionsViewModel
            {
                Years = new List<int> { currentYear - 6 },
                PagingDetail = new PagingDetail
                {
                    CurrentPage = 2,
                    PageSize = 5,
                    TotalItems = 6,
                    PagingLink = $"{PagePaths.FileUploadHistoryPreviousSubmissions}?page="
                }
            });
        }

        [Test]
        public async Task Get_ReturnsRedirect_WhenPreviousDataDoesNotExist()
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear },
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(submissionIds);

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(nameof(FileUploadSubmissionHistoryController.Get));
            result.ControllerName.Should().Be(nameof(FileUploadSubmissionHistoryController).RemoveControllerFromName());
        }

        [Test]
        [TestCase(-1, 1)]
        [TestCase(3, 2)]
        public async Task Get_ReturnsRedirect_WhenInvalidPageParameterIsPassed(int page, int expectedRedirectPage)
        {
            // Arrange
            var currentYear = DateTime.Now.Year;

            var submissionIds = new List<SubmissionPeriodId>
            {
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 1 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 2 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 3 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 4 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 5 },
                new SubmissionPeriodId { SubmissionId = Guid.NewGuid(), SubmissionPeriod = "July to December", Year = currentYear - 6 },
            };

            _submissionServiceMock.Setup(x => x.GetSubmissionIdsAsync(_organisationId, SubmissionType.Producer, null, null))
                .ReturnsAsync(submissionIds);

            _sessionMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession());

            // Act
            var result = await _systemUnderTest.Get(page) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(nameof(FileUploadHistoryPreviousSubmissionsController.Get));
            result.ControllerName.Should().BeNull();
            result.RouteValues["page"].Should().Be(expectedRedirectPage);
        }
    }
}
