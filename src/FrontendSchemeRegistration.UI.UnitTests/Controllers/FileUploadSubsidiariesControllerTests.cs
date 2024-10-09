using System.Security.Claims;
using System.Text.Json;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    using Application.DTOs;
    using Application.DTOs.ComplianceScheme;
    using Application.DTOs.ComplianceSchemeMember;
    using Application.DTOs.Subsidiary.OrganisationSubsidiaryList;
    using Application.Options;
    using Constants;
    using EPR.Common.Authorization.Sessions;
    using FrontendSchemeRegistration.Application.Constants;
    using FrontendSchemeRegistration.Application.DTOs.Organisation;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using UI.Sessions;

    [TestFixture]
    public class FileUploadSubsidiariesControllerTests
    {
        private readonly Mock<ClaimsPrincipal> _userMock = new();
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ISubmissionService> _mockSubmissionService;
        private Mock<ISubsidiaryService> _mockSubsidiaryService;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _mockSessionManager;
        private Mock<IComplianceSchemeMemberService> _mockComplianceSchemeMemberService;
        private FileUploadSubsidiariesController _controller;
        private Mock<IOptions<GlobalVariables>> _globalVariablesMock;
        private Mock<ClaimsPrincipal> _claimsPrincipalMock;
        private Mock<IUrlHelper> _mockUrlHelper;
        private readonly Guid UserId = Guid.NewGuid();

        [SetUp]
        public void SetUp()
        {
            _mockUrlHelper = new Mock<IUrlHelper>();
            _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns<string>(url => !string.IsNullOrEmpty(url));

            var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
            _userMock.Setup(x => x.Claims).Returns(claims);

            _mockFileUploadService = new Mock<IFileUploadService>();
            _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubmissionService = new Mock<ISubmissionService>();
            _mockSubsidiaryService = new Mock<ISubsidiaryService>();
            _mockSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            _mockSessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession
                {
                    RegistrationSession = new RegistrationSession { SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() } }
                });

            _mockComplianceSchemeMemberService = new Mock<IComplianceSchemeMemberService>();
            _globalVariablesMock = new Mock<IOptions<GlobalVariables>>();
            _globalVariablesMock.Setup(g => g.Value).Returns(new GlobalVariables()
            {
                BasePath = "/",
            });

            var complianceApiResponse = new ComplianceSchemeMembershipResponse
            {
                PagedResult = new PaginatedResponse<ComplianceSchemeMemberDto>
                {
                    Items = new List<ComplianceSchemeMemberDto>
                    {
                        new()
                        {
                            OrganisationName = "Test Organisation Name",
                            OrganisationNumber = "0123456789",
                            Relationships = new List<RelationshipResponseModel>
                            {
                                new() { OrganisationNumber = "987654321", OrganisationName = "Subsidiary1" },
                                new() { OrganisationNumber = "852147930", OrganisationName = "Subsidiary2" },
                                new() { OrganisationNumber = "741229428", OrganisationName = "Subsidiary3" },
                            }
                        }
                    },
                    TotalItems = 1
                }
            };

            _mockComplianceSchemeMemberService.Setup(s => s.GetComplianceSchemeMembers(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .ReturnsAsync(complianceApiResponse);

            _controller = new FileUploadSubsidiariesController(
                _mockFileUploadService.Object,
                _mockSubmissionService.Object,
                _mockSubsidiaryService.Object,
                _globalVariablesMock.Object,
                _mockSessionManager.Object,
                _mockComplianceSchemeMemberService.Object);

            // Mock HttpContext
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();

            // Setup Request properties
            mockRequest.Setup(r => r.ContentType).Returns("multipart/form-data");
            mockRequest.Setup(r => r.Body).Returns(Stream.Null);

            // Set the HttpContext's Request to our mockRequest
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(c => c.User).Returns(_userMock.Object);

            // Assign HttpContext to ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _claimsPrincipalMock.Object,
                    Session = new Mock<ISession>().Object
                }
            };
            _controller.Url = _mockUrlHelper.Object;
        }

        [Test]
        public async Task SubsidiariesList_WhenDirectProducer_ShouldCallSubsidiaryServiceAndReturnViewResult()
        {
            // Arrange
            var orgModel = new OrganisationRelationshipModel
            {
                Organisation = new OrganisationDetailModel
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Organisation Name",
                    OrganisationNumber = "0123456789"
                },
                Relationships = new List<RelationshipResponseModel>
                {
                    new() { OrganisationNumber = "987654321", OrganisationName = "Subsidiary1" },
                    new() { OrganisationNumber = "852147930", OrganisationName = "Subsidiary2" },
                    new() { OrganisationNumber = "741229428", OrganisationName = "Subsidiary3" },
                }
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer);
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
            _mockSubsidiaryService.Setup(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>())).ReturnsAsync(orgModel);

            // Act
            var result = await _controller.SubsidiariesList();

            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
            var viewResult = (result as ViewResult).Model as SubsidiaryListViewModel;
            viewResult.Should().NotBeNull();
            viewResult.Organisations.Should().HaveCount(1);
            viewResult.Organisations[0].Subsidiaries.Should().HaveCount(3);
            _mockSubsidiaryService.Verify(service => service.GetOrganisationSubsidiaries(It.IsAny<Guid>()), Times.Once);
        }

        [Test]
        public async Task SubsidiariesList_WhenComplianceScheme_ShouldCallComplianceServiceAndReturnViewResult()
        {
            // Act
            var result = await _controller.SubsidiariesList();

            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
            var viewResult = (result as ViewResult).Model as SubsidiaryListViewModel;
            viewResult.Should().NotBeNull();
            viewResult.Organisations.Should().HaveCount(1);
            viewResult.Organisations[0].Subsidiaries.Should().HaveCount(3);
            _mockComplianceSchemeMemberService.Verify(
                s => s.GetComplianceSchemeMembers(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()), Times.Once);
        }

        [Theory]
        [TestCase(PagePaths.FileUploadSubsidiariesSuccess)]
        [TestCase(PagePaths.SubsidiariesDownload)]
        [TestCase(PagePaths.SubsidiariesDownloadFailed)]
        [TestCase(PagePaths.ConfirmSubsidiaryRemoval)]
        public async Task SubsidiariesList_WhenFromAccountLinkPage_SetAccountHomeLink(string pagePath)
        {
            // Arrange
            var mockSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            mockSessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession
                {
                    RegistrationSession = new RegistrationSession { SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() } },
                    SubsidiarySession = new SubsidiarySession { Journey = [pagePath] }
                });

            var controller = new FileUploadSubsidiariesController(
                _mockFileUploadService.Object,
                _mockSubmissionService.Object,
                _mockSubsidiaryService.Object,
                _globalVariablesMock.Object,
                mockSessionManager.Object,
                _mockComplianceSchemeMemberService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _claimsPrincipalMock.Object,
                    Session = new Mock<ISession>().Object
                }
            };
            controller.Url = _mockUrlHelper.Object;

            // Act
            var result = await controller.SubsidiariesList();

            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewData.Should().Contain(pair => 
                pair.Key == "ShouldShowAccountHomeLink" && (bool)pair.Value == true);
        }

        [Test]
        public async Task SubsidiariesList_WhenPageLessThanOne_RedirectsToPageOne()
        {
            // Act
            var result = await _controller.SubsidiariesList(0);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirect = result as RedirectToActionResult;
            redirect.ActionName.Should().Be(nameof(FileUploadSubsidiariesController.SubsidiariesList));
            redirect.RouteValues.Should().Contain(pair => pair.Key == "page" && int.Parse(pair.Value.ToString()) == 1);
        }

        [Test]
        public async Task SubsidiariesList_WhenComplianceSchemeAndNoApiResponse_ThenEmptySubsidiaryListReturned()
        {
            // Arrange
            var complianceApiResponse = new ComplianceSchemeMembershipResponse
            {
                PagedResult = null
            };

            var mockComplianceSchemeMemberService = new Mock<IComplianceSchemeMemberService>();
            mockComplianceSchemeMemberService.Setup(s => s.GetComplianceSchemeMembers(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .ReturnsAsync(complianceApiResponse);

            var controller = new FileUploadSubsidiariesController(
                _mockFileUploadService.Object,
                _mockSubmissionService.Object,
                _mockSubsidiaryService.Object,
                _globalVariablesMock.Object,
                _mockSessionManager.Object,
                mockComplianceSchemeMemberService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _claimsPrincipalMock.Object,
                    Session = new Mock<ISession>().Object
                }
            };
            controller.Url = _mockUrlHelper.Object;

            // Act
            var result = await controller.SubsidiariesList();

            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
            var viewResult = (result as ViewResult).Model as SubsidiaryListViewModel;
            viewResult.Should().NotBeNull();
            viewResult.Organisations.Should().HaveCount(1);
            viewResult.Organisations[0].Subsidiaries.Should().BeEmpty();
        }

        [Test]
        public async Task Post_WhenModelStateIsValid_ShouldRedirectToFileUploading()
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            _mockFileUploadService
                .Setup(service => service.ProcessUploadAsync(
                    It.IsAny<string?>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<SubmissionType>(),
                    It.IsAny<IFileUploadMessages>(),
                    It.IsAny<IFileUploadSize>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(submissionId);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.ContentType).Returns("multipart/form-data");
            mockHttpContext.Setup(c => c.Request.Body).Returns(Stream.Null);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.Post();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("FileUploading");
            result.As<RedirectToActionResult>().RouteValues["submissionId"].Should().Be(submissionId);
        }


        [Test]
        public async Task Post_WhenModelStateIsValid_ShouldRedirectToFileUploading_WithNoComplianceScheme()
        {
            // Arrange
            var submissionId = Guid.NewGuid();

            _mockFileUploadService
                .Setup(service => service.ProcessUploadAsync(
                    It.IsAny<string?>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<SubmissionType>(),
                    It.IsAny<IFileUploadMessages>(),
                    It.IsAny<IFileUploadSize>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(submissionId);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.ContentType).Returns("multipart/form-data");
            mockHttpContext.Setup(c => c.Request.Body).Returns(Stream.Null);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            _mockSessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession
                {
                    RegistrationSession = new RegistrationSession { SelectedComplianceScheme = null }
                });

            // Act
            var result = await _controller.Post();

            // Assert
            _mockFileUploadService.Verify(
                s => s.ProcessUploadAsync(
                    It.IsAny<string?>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<SubmissionType>(),
                    It.IsAny<IFileUploadMessages>(),
                    It.IsAny<IFileUploadSize>(),
                    It.Is<Guid?>(g => g == null)),
                Times.Once);
        }

        [Test]
        public async Task Post_WhenModelStateIsValid_ShouldRedirectToFileUploading_WithComplianceSchemeId()
        {
            // Arrange
            var complianceSchemeId = Guid.NewGuid();
            var submissionId = Guid.NewGuid();

            _mockFileUploadService
                .Setup(service => service.ProcessUploadAsync(
                    It.IsAny<string?>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<SubmissionType>(),
                    It.IsAny<IFileUploadMessages>(),
                    It.IsAny<IFileUploadSize>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(submissionId);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.ContentType).Returns("multipart/form-data");
            mockHttpContext.Setup(c => c.Request.Body).Returns(Stream.Null);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            _mockSessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
                .ReturnsAsync(new FrontendSchemeRegistrationSession
                {
                    RegistrationSession = new RegistrationSession { SelectedComplianceScheme = new ComplianceSchemeDto { Id = complianceSchemeId } }
                });

            // Act
            var result = await _controller.Post();

            // Assert
            _mockFileUploadService.Verify(
                s => s.ProcessUploadAsync(
                    It.IsAny<string?>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<SubmissionType>(),
                    It.IsAny<IFileUploadMessages>(),
                    It.IsAny<IFileUploadSize>(),
                    It.Is<Guid?>(c => c == complianceSchemeId)),
                Times.Once);
        }

        [Test]
        public async Task Post_WhenModelStateIsInvalid_ShouldReturnViewResult()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Test error");

            // Act
            var result = await _controller.Post();

            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("SubsidiariesList");
        }

        [Test]
        public async Task FileUploading_WhenSubmissionIsNull_ShouldRedirectToFileUploadGet()
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            _mockSubmissionService.Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>())).ReturnsAsync((SubsidiarySubmission)null);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUploading();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Get");
            result.As<RedirectToActionResult>().ControllerName.Should().Be("FileUpload");
        }

        [Test]
        public async Task FileUploading_WhenSubmissionIsComplete_ShouldRedirectToFileUploadSuccess()
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var submission = new SubsidiarySubmission
            {
                SubsidiaryDataComplete = true,
                RecordsAdded = 5,
                Errors = new List<string>()
            };
            _mockSubmissionService.Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUploading();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("FileUploadSuccess");
            result.As<RedirectToActionResult>().RouteValues["recordsAdded"].Should().Be(submission.RecordsAdded);
        }

        [Test]
        public async Task FileUploadSuccess_ShouldReturnViewResultWithModel()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.Request.Query["recordsAdded"]).Returns("5");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUploadSuccess();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("FileUploadSuccess");
            var model = viewResult.Model.Should().BeOfType<SubsidiaryFileUploadSuccessViewModel>().Subject;
            model.RecordsAdded.Should().Be(5);
        }
        [Test]
        public async Task SubsidiariesDownload_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true)).ReturnsAsync(mockStream);
            _controller.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = await _controller.SubsidiariesDownload() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("SubsidiariesDownloadView");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true), Times.Never);
        }

        [Test]
        public async Task SubsidiariesDownloadView_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true)).ReturnsAsync(mockStream);
            _controller.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = _controller.SubsidiariesDownloadView();

            // Assert
            ((Microsoft.AspNetCore.Mvc.ViewResult)result).ViewName.Should().Be("SubsidiariesDownload");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true), Times.Never);
        }

        [Test]
        public async Task ExportSubsidiaries_ReturnsFileResultWithCorrectContentTypeAndFileName()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true)).ReturnsAsync(mockStream);
            _controller.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = await _controller.ExportSubsidiaries();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileResult = result as FileStreamResult;
            fileResult.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().Be("subsidiary.csv");
            fileResult.FileStream.Should().BeSameAs(mockStream);

            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true), Times.Once);
        }

        [Test]
        public async Task ExportSubsidiaries_WhenError_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true)).ThrowsAsync(new Exception("Some message"));

            // Act
            var result = await _controller.ExportSubsidiaries() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("SubsidiariesDownloadFailed");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true), Times.Once);
        }

        [Test]
        public async Task ExportSubsidiaries_WhenStreamIsNull_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true)).ReturnsAsync((MemoryStream)null);

            // Act
            var result = await _controller.ExportSubsidiaries() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("SubsidiariesDownloadFailed");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true), Times.Once);
        }

        [Test]
        public async Task ConfirmRemoveSubsidiary_SelectedConfirmRemovalYes_TerminatesSubsidiaryAndRedirects()
        {
            // Arrange
            var model = new SubsidiaryConfirmRemovalViewModel
            {
                SelectedConfirmRemoval = YesNoAnswer.Yes,
                ParentOrganisationExternalId = Guid.NewGuid(),
                SubsidiaryExternalId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.ConfirmRemoveSubsidiary(model);

            // Assert
            _mockSubsidiaryService.Verify(s => s.TerminateSubsidiary(
                model.ParentOrganisationExternalId, model.SubsidiaryExternalId, UserId), Times.Once);

            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(_controller.SubsidiariesList));
        }

        [Test]
        public async Task ConfirmRemoveSubsidiary_SelectedConfirmRemovalNo_RedirectsWithoutTerminating()
        {
            // Arrange
            var model = new SubsidiaryConfirmRemovalViewModel
            {
                SelectedConfirmRemoval = YesNoAnswer.No,
                ParentOrganisationExternalId = Guid.NewGuid(),
                SubsidiaryExternalId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.ConfirmRemoveSubsidiary(model);

            // Assert
            _mockSubsidiaryService.Verify(s => s.TerminateSubsidiary(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);

            result.Should().BeOfType<RedirectToActionResult>()
              .Which.ActionName.Should().Be(nameof(_controller.SubsidiariesList));
        }

        [Test]
        public async Task ConfirmRemoveSubsidiary_InvalidSelection_ReturnsViewWithModel()
        {
            // Arrange
            var model = new SubsidiaryConfirmRemovalViewModel
            {
                SelectedConfirmRemoval = (YesNoAnswer)999, // Invalid value
                ParentOrganisationExternalId = Guid.NewGuid(),
                SubsidiaryExternalId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.ConfirmRemoveSubsidiary(model);

            // Assert
            _mockSubsidiaryService.Verify(s => s.TerminateSubsidiary(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);

            result.Should().BeOfType<ViewResult>()
                .Which.Model.Should().Be(model);
        }


        [Test]
        public async Task ConfirmRemoveSubsidiary_UserNotInRole_ReturnsUnauthorizedResult()
        {
            // Arrange
            var subsidiaryReference = "validSubsidiaryRef";
            var parentOrganisationExternalId = Guid.NewGuid();

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Basic User");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            // Act
            var result = await _controller.ConfirmRemoveSubsidiary(subsidiaryReference, parentOrganisationExternalId);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Test]
        public async Task ConfirmRemoveSubsidiary_UserInRole_ReturnsViewWithModel()
        {
            // Arrange
            var subsidiaryReference = "validSubsidiaryRef";
            var parentOrganisationExternalId = Guid.NewGuid();
            var subsidiaryDetails = new OrganisationDto
            {
                Name = "Subsidiary Name",
                ExternalId = Guid.NewGuid()
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
            _mockSubsidiaryService.Setup(s => s.GetOrganisationByReferenceNumber(subsidiaryReference))
                .ReturnsAsync(subsidiaryDetails);

            // Act
            var result = await _controller.ConfirmRemoveSubsidiary(subsidiaryReference, parentOrganisationExternalId);

            // Assert
            result.Should().BeOfType<ViewResult>()
                .Which.Model.Should().BeOfType<SubsidiaryConfirmRemovalViewModel>()
                .Which.Should().Match<SubsidiaryConfirmRemovalViewModel>(model =>
                    model.SubsidiaryName == subsidiaryDetails.Name &&
                    model.SubsidiaryExternalId == subsidiaryDetails.ExternalId &&
                    model.ParentOrganisationExternalId == parentOrganisationExternalId);
        }

        [Test]
        public async Task SubsidiariesDownloadFailed_ReturnsSuccessfully()
        {
            // Act
            var result = await _controller.SubsidiariesDownloadFailed();

            // Assert
            result.Should().BeOfType<ViewResult>()
                .Which.ViewName.Should().Be(nameof(_controller.SubsidiariesDownloadFailed));
        }

        private List<Claim> CreateUserDataClaim(string organisationRole, string serviceRole = null)
        {
            var userData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrganisationRole = organisationRole,
                        Name = "Test Name",
                        OrganisationNumber = "Test Number"
                    }
                },
                Id = UserId,
                ServiceRole = serviceRole
            };

            return new List<Claim>
            {
                new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
            };
        }
    }
}