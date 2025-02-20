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
    using FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
    using Microsoft.AspNetCore.Mvc.Routing;
    using FrontendSchemeRegistration.Application.Constants;
    using FrontendSchemeRegistration.Application.DTOs.Organisation;
    using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.FeatureManagement;
    using UI.Sessions;

    [TestFixture]
    public class FileUploadSubsidiariesControllerTests
    {
        private const string DummySubsidiaryName = "DummySubsidiaryName";
        private const string SelfReportingType = "Self";
        private const string GroupReportingType = "Group";

        private readonly Mock<ClaimsPrincipal> _userMock = new();
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ISubmissionService> _mockSubmissionService;
        private Mock<ISubsidiaryService> _mockSubsidiaryService;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _mockSessionManager;
        private Mock<IComplianceSchemeMemberService> _mockComplianceSchemeMemberService;
        private Mock<IComplianceSchemeService> _mockComplianceSchemeService;
        private FileUploadSubsidiariesController _controller;
        private Mock<IOptions<GlobalVariables>> _globalVariablesMock;
        private Mock<ClaimsPrincipal> _claimsPrincipalMock;
        private Mock<IFeatureManager> _mockFeatureManager;
        private Mock<IUrlHelper> _mockUrlHelper;
        private Mock<ISubsidiaryUtilityService> _mockSubsidiaryUtilityService;
        private readonly Guid UserId = Guid.NewGuid();
        private readonly Guid OrganisationId = Guid.NewGuid();
        private readonly DateTime JoinerDate = new DateTime(2024, 12, 17);


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
                    RegistrationSession = new RegistrationSession { SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() } },
                    SubsidiarySession = new SubsidiarySession { ReturnToSubsidiaryPage = 5 }
                });

            _mockComplianceSchemeMemberService = new Mock<IComplianceSchemeMemberService>();
            _mockComplianceSchemeService = new Mock<IComplianceSchemeService>();
            _mockComplianceSchemeService
                .Setup(service => service.GetComplianceSchemeSummary(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(() => new ComplianceSchemeSummary { MemberCount = 3 });

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
                                new() { OrganisationNumber = "987654321", OrganisationName = "Subsidiary1" , JoinerDate = JoinerDate, ReportingType = SelfReportingType},
                                new() { OrganisationNumber = "852147930", OrganisationName = "Subsidiary2" , JoinerDate = JoinerDate, ReportingType = GroupReportingType},
                                new() { OrganisationNumber = "741229428", OrganisationName = "Subsidiary3" , JoinerDate = JoinerDate, ReportingType = GroupReportingType},
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
                    It.IsAny<int>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(complianceApiResponse);
            _mockSubsidiaryUtilityService = new Mock<ISubsidiaryUtilityService>();

            _mockFeatureManager = new Mock<IFeatureManager>();            

            _controller = new FileUploadSubsidiariesController(
                _mockFileUploadService.Object,
                _mockSubmissionService.Object,
                _mockSubsidiaryService.Object,
                _globalVariablesMock.Object,
                _mockSessionManager.Object,
                _mockComplianceSchemeMemberService.Object,
                _mockComplianceSchemeService.Object,
                _mockSubsidiaryUtilityService.Object,
                _mockFeatureManager.Object);

            var tempDataMock = new Mock<ITempDataDictionary>();
            tempDataMock.Setup(dictionary => dictionary["SubsidiaryNameToRemove"]).Returns(DummySubsidiaryName);
            _controller.TempData = tempDataMock.Object;
            
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
                    Session = new Mock<ISession>().Object,
                    
                }
            };
            _controller.Url = _mockUrlHelper.Object;
        }

        [Test]
        public async Task SubsidiariesList_WhenDirectProducer_ShouldCallSubsidiaryServiceAndReturnViewResult()
        {
            // Arrange
            _mockSubsidiaryService.Setup(x => x.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.NoFileUploadActive);

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
                    new() { OrganisationNumber = "987654321", OrganisationName = "Subsidiary1" , JoinerDate = JoinerDate, ReportingType = SelfReportingType},
                    new() { OrganisationNumber = "852147930", OrganisationName = "Subsidiary2" , JoinerDate = JoinerDate, ReportingType = GroupReportingType},
                    new() { OrganisationNumber = "741229428", OrganisationName = "Subsidiary3" , JoinerDate = JoinerDate, ReportingType = GroupReportingType},
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

            var subsidiary = viewResult.Organisations[0].Subsidiaries.First();
            subsidiary.JoinerDate.Should().Be(JoinerDate); 
            subsidiary.ReportingType.Should().Be(SelfReportingType); 

            _mockSubsidiaryService.Verify(service => service.GetOrganisationSubsidiaries(It.IsAny<Guid>()), Times.Once);
        }

        [Test]
        public async Task SubsidiariesList_WhenDirectProducer_WithNullOrganisation_ShouldCallSubsidiaryServiceAndReturnViewResult()
        {
            // Arrange
            var claims = CreateUserDataClaim(OrganisationRoles.Producer);
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
            _mockSubsidiaryService.Setup(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>())).ReturnsAsync(() => null);
            _mockSubsidiaryService.Setup(service => service.GetSubsidiaryFileUploadStatusViewedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
            
            // Act
            var result = await _controller.SubsidiariesList();

            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
            var viewResult = (result as ViewResult).Model as SubsidiaryListViewModel;
            viewResult.Should().NotBeNull();
            viewResult.Organisations.Should().HaveCount(1);
            viewResult.Organisations[0].Subsidiaries.Should().HaveCount(0);
            _mockSubsidiaryService.Verify(service => service.GetOrganisationSubsidiaries(It.IsAny<Guid>()), Times.Once);
        }

        [Test]
        public async Task SubsidiariesList_WhenComplianceScheme_ShouldCallComplianceServiceAndReturnViewResult()
        {
            // Arrange
            _mockSubsidiaryService.Setup(x => x.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.NoFileUploadActive);
            
            
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
                    It.IsAny<int>(),
                    It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task SubsidiariesList_WhenComplianceScheme_ShouldCallComplianceServiceAndReturnViewResultWith_JoinerDate_ReportingType()
        {
            // Arrange
            _mockSubsidiaryService.Setup(x => x.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.NoFileUploadActive);

            // Act
            var result = await _controller.SubsidiariesList();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var viewModel = viewResult.Model.Should().BeOfType<SubsidiaryListViewModel>().Subject;

            // Validate first subsidiary
            var firstSubsidiary = viewModel.Organisations[0].Subsidiaries.First();
            firstSubsidiary.JoinerDate.Should().Be(JoinerDate);
            firstSubsidiary.ReportingType.Should().Be(SelfReportingType);
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
                _mockComplianceSchemeMemberService.Object,
                _mockComplianceSchemeService.Object,
                _mockSubsidiaryUtilityService.Object,
                _mockFeatureManager.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _claimsPrincipalMock.Object,
                    Session = new Mock<ISession>().Object
                }
            };
            controller.Url = _mockUrlHelper.Object;
            _mockSubsidiaryService.Setup(service => service.GetSubsidiaryFileUploadStatusViewedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
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
            _mockSubsidiaryService.Setup(x => x.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.NoFileUploadActive);

            // Arrange
            var complianceApiResponse = new ComplianceSchemeMembershipResponse
            {
                PagedResult = new PaginatedResponse<ComplianceSchemeMemberDto>()
                {
                    Items = []
                }
            };

            var mockComplianceSchemeMemberService = new Mock<IComplianceSchemeMemberService>();
            mockComplianceSchemeMemberService.Setup(s => s.GetComplianceSchemeMembers(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(complianceApiResponse);

            var controller = new FileUploadSubsidiariesController(
                _mockFileUploadService.Object,
                _mockSubmissionService.Object,
                _mockSubsidiaryService.Object,
                _globalVariablesMock.Object,
                _mockSessionManager.Object,
                mockComplianceSchemeMemberService.Object,
                _mockComplianceSchemeService.Object,
                _mockSubsidiaryUtilityService.Object,
                _mockFeatureManager.Object);

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
        public async Task SubsidiariesList_WhenComplianceSchemePage2RequestedButDoesNotExist_ThenPage1DataReturn()
        {
            // Arrange
            _mockSubsidiaryService.Setup(service => service.GetSubsidiaryFileUploadStatusViewedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

            // Act
            var result = await _controller.SubsidiariesList(2);
            
            // Assert
            result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
            var viewResult = (result as ViewResult).Model as SubsidiaryListViewModel;
            viewResult.Should().NotBeNull();
            viewResult.PagingDetail.CurrentPage.Should().Be(1);
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
        public async Task FileUploading_FileUploadedSuccessfully_ReturnsRedirectToAction_FileUploadSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var submissionId = Guid.NewGuid();
            var subFileUploadViewModel = new SubFileUploadingViewModel { SubmissionId = submissionId.ToString() };
            var submission = new SubsidiarySubmission
            {
                SubsidiaryFileUploadDateTime = DateTime.Now.AddMinutes(-3)
            };

            var uploadStatus = new SubsidiaryUploadStatusDto
            {
                Status = SubsidiaryUploadStatus.Finished,
                RowsAdded = 1
            };

            _mockSubmissionService.Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(s => s.GetUploadStatus(UserId, OrganisationId)).ReturnsAsync(uploadStatus);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(_claimsPrincipalMock.Object);
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            mockHttpContext.Setup(c => c.User).Returns(_claimsPrincipalMock.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };
            // Act
            var result = await _controller.FileUploading();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(_controller.FileUploadSuccess));
        }

        [TestCase(0, false)]
        [TestCase(-10, true)]
        public async Task FileUploading_WhenSubmissionIsInProcess_ShouldReturnCorrectView(int uploadMinutesToAdd, bool expectedIsFileTakingLongToUpload)
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var submission = new SubsidiarySubmission
            {
                SubsidiaryDataComplete = true,
                RecordsAdded = 5,
                Errors = new List<string>(),
                SubsidiaryFileUploadDateTime = DateTime.UtcNow.AddMinutes(uploadMinutesToAdd)
            };

            var uploadStatus = new SubsidiaryUploadStatusDto
            {
                Status = SubsidiaryUploadStatus.Uploading,
                RowsAdded = null,
                Errors = null
            };

            _mockSubmissionService.Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(s => s.GetUploadStatus(UserId, OrganisationId)).ReturnsAsync(uploadStatus);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(_claimsPrincipalMock.Object);
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedViewModel = new SubFileUploadingViewModel
            {
                SubmissionId = submissionId.ToString(),
                IsFileUploadTakingLong = expectedIsFileTakingLongToUpload
            };

            // Act
            var result = await _controller.FileUploading() as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploading");
            result.ViewData.Model.Should().BeEquivalentTo(expectedViewModel);
        }

        [TestCase(0, nameof(FileUploadSubsidiariesController.SubsidiariesFileNotUploaded))]
        [TestCase(1, nameof(FileUploadSubsidiariesController.SubsidiariesIncompleteFileUpload))]
        public async Task FileUploading_WhenSubmissionHasErrors_ShouldReturnCorrectRedirect(int rowsAdded, string actionName)
        {
            // Arrange
            var submissionId = Guid.NewGuid();
            var submission = new SubsidiarySubmission
            {
                SubsidiaryDataComplete = true,
                RecordsAdded = 5,
                Errors = new List<string>()
            };

            var uploadStatus = new SubsidiaryUploadStatusDto
            {
                Status = SubsidiaryUploadStatus.Finished,
                RowsAdded = rowsAdded,
                Errors = new SubsidiaryUploadErrorDto[]
                {
                    new SubsidiaryUploadErrorDto()
                }
            };

            _mockSubmissionService.Setup(service => service.GetSubmissionAsync<SubsidiarySubmission>(It.IsAny<Guid>())).ReturnsAsync(submission);

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(s => s.GetUploadStatus(UserId, OrganisationId)).ReturnsAsync(uploadStatus);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(_claimsPrincipalMock.Object);
            mockHttpContext.Setup(c => c.Request.Query["submissionId"]).Returns(submissionId.ToString());
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.FileUploading() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(actionName);
            result.ControllerName.Should().BeNull();
        }

        [Test]
        public async Task FileUploadSuccess_ShouldReturnViewResultWithModel()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
                       
            mockHttpContext.Setup(c => c.User).Returns(_claimsPrincipalMock.Object);
            mockHttpContext.Setup(c => c.Session).Returns(new Mock<ISession>().Object);

            var expectedUploadStatus = new SubsidiaryUploadStatusDto 
            {
                Status = "Completed",
                RowsAdded = 5
            };

            _mockSubsidiaryService.Setup(service => service.GetUploadStatus(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(expectedUploadStatus);

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

        [TestCase(1, "1B")]
        [TestCase(1024, "1KB")]
        [TestCase(1048576, "1MB")]
        public async Task SubsidiariesIncompleteFileUpload_WhenCalled_ShouldReturnCorrectView(long size, string expectedSizeToDisplay)
        {
            // Arrange
            var bytes = new byte[size];

            var expectedViewModel = new SubsidiariesUnsuccessfulFileUploadViewModel
            {
                PartialSuccess = true,
                WarningsReportDisplaySize = expectedSizeToDisplay
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(x => x.GetUploadErrorsReport(UserId, OrganisationId)).ReturnsAsync(new MemoryStream(bytes));

            // Act
            var result = await _controller.SubsidiariesIncompleteFileUpload() as ViewResult;

            // Assert
            result.ViewName.Should().Be("SubsidiariesUnsuccessfulFileUpload");
            result.ViewData.Model.Should().BeEquivalentTo(expectedViewModel);
            _mockSubsidiaryService.Verify(
                s => s.SetSubsidiaryFileUploadStatusViewedAsync(true, It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Once);
        }

        [TestCaseSource(nameof(SubsidiariesUnsuccessfulFileUploadDecisionCases))]
        public async Task SubsidiariesIncompleteFileUploadDecision_WhenCalled_ShouldReturnCorrectRedirect(
            bool uploadNewFile,
            string actionName,
            string controllerName,
            RouteValueDictionary routeValues)
        {
            // Act
            var result = await _controller.SubsidiariesIncompleteFileUploadDecision(new SubsidiaryUnsuccessfulUploadDecisionViewModel
            {
                UploadNewFile = uploadNewFile
            }) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(actionName);
            result.ControllerName.Should().Be(controllerName);
            result.RouteValues.Should().BeEquivalentTo(routeValues);
        }

        [Test]
        public async Task SubsidiariesIncompleteFileUploadDecision_WhenCalled_ShouldReturnCorrectView()
        {
            // Arrange
            var bytes = new byte[1];

            _controller.ModelState.AddModelError("test", "test");

            var expectedViewModel = new SubsidiariesUnsuccessfulFileUploadViewModel
            {
                PartialSuccess = true,
                WarningsReportDisplaySize = "1B"
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(x => x.GetUploadErrorsReport(UserId, OrganisationId)).ReturnsAsync(new MemoryStream(bytes));

            // Act
            var result = await _controller.SubsidiariesIncompleteFileUploadDecision(new SubsidiaryUnsuccessfulUploadDecisionViewModel()) as ViewResult;

            // Assert
            result.ViewName.Should().Be("SubsidiariesUnsuccessfulFileUpload");
            result.ViewData.Model.Should().BeEquivalentTo(expectedViewModel);
            _mockSubsidiaryService.Verify(x => x.GetUploadErrorsReport(UserId, OrganisationId), Times.Once);
        }

        [Test]
        public async Task SubsidiariesFileNotUploaded_WhenCalled_ShouldReturnCorrectView()
        {
            // Arrange
            var bytes = new byte[1];

            var expectedViewModel = new SubsidiariesUnsuccessfulFileUploadViewModel
            {
                PartialSuccess = false,
                WarningsReportDisplaySize = "1B"
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(x => x.GetUploadErrorsReport(UserId, OrganisationId)).ReturnsAsync(new MemoryStream(bytes));

            // Act
            var result = await _controller.SubsidiariesFileNotUploaded() as ViewResult;

            // Assert
            result.ViewName.Should().Be("SubsidiariesUnsuccessfulFileUpload");
            result.ViewData.Model.Should().BeEquivalentTo(expectedViewModel);
        }

        [Test]
        public async Task SubsidiariesFileNotUploaded_ShouldCallSetSubsidiaryFileUploadStatusViewedAsync_WithCorrectParameters()
        {
            // Arrange
            var bytes = new byte[1];

            var expectedViewModel = new SubsidiariesUnsuccessfulFileUploadViewModel
            {
                PartialSuccess = false,
                WarningsReportDisplaySize = "1B"
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(x => x.GetUploadErrorsReport(UserId, OrganisationId)).ReturnsAsync(new MemoryStream(bytes));

            // Act
            var result = await _controller.SubsidiariesFileNotUploaded();

            // Assert
            _mockSubsidiaryService.Verify(
                s => s.SetSubsidiaryFileUploadStatusViewedAsync(true, It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Once
            );
        }

        [TestCaseSource(nameof(SubsidiariesUnsuccessfulFileUploadDecisionCases))]
        public async Task SubsidiariesFileNotUploadedDecision_WhenCalled_ShouldReturnCorrecRedirect(
            bool uploadNewFile,
            string actionName,
            string controllerName,
            RouteValueDictionary routeValues)
        {
            // Act
            var result = await _controller.SubsidiariesFileNotUploadedDecision(new SubsidiaryUnsuccessfulUploadDecisionViewModel
            {
                UploadNewFile = uploadNewFile
            }) as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be(actionName);
            result.ControllerName.Should().Be(controllerName);
            result.RouteValues.Should().BeEquivalentTo(routeValues);
        }

        [Test]
        public async Task SubsidiariesFileNotUploadedDecision_WhenCalled_ShouldReturnCorrectView()
        {
            // Arrange
            var bytes = new byte[1];

            _controller.ModelState.AddModelError("test", "test");

            var expectedViewModel = new SubsidiariesUnsuccessfulFileUploadViewModel
            {
                PartialSuccess = false,
                WarningsReportDisplaySize = "1B"
            };

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(x => x.GetUploadErrorsReport(UserId, OrganisationId)).ReturnsAsync(new MemoryStream(bytes));

            // Act
            var result = await _controller.SubsidiariesFileNotUploadedDecision(new SubsidiaryUnsuccessfulUploadDecisionViewModel()) as ViewResult;

            // Assert
            result.ViewName.Should().Be("SubsidiariesUnsuccessfulFileUpload");
            result.ViewData.Model.Should().BeEquivalentTo(expectedViewModel);
            _mockSubsidiaryService.Verify(x => x.GetUploadErrorsReport(UserId, OrganisationId), Times.Once);
        }

        [Test]
        public async Task SubsidiariesFileUploadWarningsReport_ReturnsStream_WhenCalled()
        {
            // Arrange
            var bytes = new byte[1];

            var claims = CreateUserDataClaim(OrganisationRoles.Producer, "Approved Person");
            _userMock.Setup(x => x.Claims).Returns(claims);
            _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

            _mockSubsidiaryService.Setup(x => x.GetUploadErrorsReport(UserId, OrganisationId)).ReturnsAsync(new MemoryStream(bytes));

            // Act
            var result = await _controller.SubsidiariesFileUploadWarningsReport() as FileStreamResult;

            // Assert
            result.ContentType.Should().Be("text/csv");
            result.FileDownloadName.Should().Be("subsidiary_validation_report.csv");
            result.FileStream.ReadByte().Should().Be(bytes[0]);
            result.FileStream.ReadByte().Should().Be(-1);

            _mockSubsidiaryService.Verify(x => x.GetUploadErrorsReport(UserId, OrganisationId), Times.Once);
        }

        [Test]
        public async Task SubsidiariesDownload_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>())).ReturnsAsync(mockStream);

            // Act
            var result = await _controller.SubsidiariesDownload() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("SubsidiariesDownloadView");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task SubsidiariesDownloadView_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>())).ReturnsAsync(mockStream);

            // Act
            var result = _controller.SubsidiariesDownloadView();

            // Assert
            ((Microsoft.AspNetCore.Mvc.ViewResult)result).ViewName.Should().Be("SubsidiariesDownload");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>()), Times.Never);
        }
                
        [Test]
        public async Task ExportSubsidiaries_ReturnsFileResultWithCorrectContentTypeAndFileName_When_EnableSubsidiaryJoinerAndLeaverColumns_Is_True()
        {
            // Arrange
            var mockStream = new MemoryStream();

            _mockFeatureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.EnableSubsidiaryJoinerAndLeaverColumns))).ReturnsAsync(true);
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(mockStream);

            // Act
            var result = await _controller.ExportSubsidiaries();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileResult = result as FileStreamResult;
            fileResult.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().Be("subsidiary.csv");
            fileResult.FileStream.Should().BeSameAs(mockStream);

            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), true), Times.Once);
        }

        [Test]
        public async Task ExportSubsidiaries_ReturnsFileResultWithCorrectContentTypeAndFileName_When_EnableSubsidiaryJoinerAndLeaverColumns_Is_False()
        {
            // Arrange
            var mockStream = new MemoryStream();

            _mockFeatureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.EnableSubsidiaryJoinerAndLeaverColumns))).ReturnsAsync(false);
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(mockStream);

            // Act
            var result = await _controller.ExportSubsidiaries();

            // Assert
            result.Should().BeOfType<FileStreamResult>();
            var fileResult = result as FileStreamResult;
            fileResult.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().Be("subsidiary.csv");
            fileResult.FileStream.Should().BeSameAs(mockStream);

            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), false), Times.Once);
        }

        [Test]
        public async Task SubsidiariesList_FileUploadedSuccessfully_RedirectsToFileUploadSuccess()
        {
            // Arrange
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.FileUploadedSuccessfully);

            // Act
            var result = await _controller.SubsidiariesList(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
               .Which.ActionName.Should().Be(nameof(_controller.FileUploadSuccess));
        }

        [Test]
        public async Task SubsidiariesList_HasErrors_RedirectsToSubsidiariesFileNotUploaded()
        {
            // Arrange
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.HasErrors);

            // Act
            var result = await _controller.SubsidiariesList(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(_controller.SubsidiariesFileNotUploaded));
        }

        [Test]
        public async Task SubsidiariesList_HasPartialErrors_RedirectsToSubsidiariesIncompleteFileUpload()
        {
            // Arrange
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.PartialUpload);

            // Act
            var result = await _controller.SubsidiariesList(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(_controller.SubsidiariesIncompleteFileUpload));
        }

        [Test]
        public async Task SubsidiariesList_FileUploadInProgress_ReturnFlagInViewModel()
        {
            // Arrange
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.FileUploadInProgress);

            // Act
            var result = await _controller.SubsidiariesList(1);

            // Assert
            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();

            var model = viewResult.Model as SubsidiaryListViewModel;
            model.Should().NotBeNull();
            model!.IsFileUploadInProgress.Should().BeTrue("because the file upload is in progress");

        }

        [Test]
        public async Task CheckFileUploadStatus_FileUploadedSuccessfully_ReturnsRedirectToFileUploadSuccess()
        {
            // Arrange
            
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.FileUploadedSuccessfully);

            var expectedRedirectUrl = "expectedRedirectUrl";
       
            var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                         .Returns(expectedRedirectUrl);
            _controller.Url = urlHelperMock.Object;

            // Act
            var result = await _controller.CheckFileUploadSatus();

            result.Should().BeOfType<JsonResult>()
                 .Which.Value.Should().BeAssignableTo<object>() 
                 .Which.Should().BeEquivalentTo(new { redirectUrl = expectedRedirectUrl });
        }

        [Test]
        public async Task CheckFileUploadStatus_HasErrors_ReturnsRedirectToFileUploadFailed()
        {
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.HasErrors);

            var expectedRedirectUrl = "SubsidiariesFileNotUploaded";
            var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                         .Returns(expectedRedirectUrl);

           _controller.Url = urlHelperMock.Object;
            // Act
            var result = await _controller.CheckFileUploadSatus();

            result.Should().BeOfType<JsonResult>()
                      .Which.Value.Should().BeAssignableTo<object>() 
                      .Which.Should().BeEquivalentTo(new { redirectUrl = expectedRedirectUrl });
        }

        [Test]
        public async Task CheckFileUploadStatus_PartialUpload_ReturnsRedirectToSubsidiariesIncompleteFileUpload()
        {
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.PartialUpload);

            var expectedRedirectUrl = "SubsidiariesIncompleteFileUpload";
            var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
            urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                         .Returns(expectedRedirectUrl);

            _controller.Url = urlHelperMock.Object;
            // Act
            var result = await _controller.CheckFileUploadSatus();

            result.Should().BeOfType<JsonResult>()
                      .Which.Value.Should().BeAssignableTo<object>()
                      .Which.Should().BeEquivalentTo(new { redirectUrl = expectedRedirectUrl });
        }

        [Test]
        public async Task CheckFileUploadStatus_FileUploadInProgress_ReturnsIsFileUploadInProgressTrue()
        {
            // Arrange
            _mockSubsidiaryService
                .Setup(s => s.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.FileUploadInProgress);

            // Act
            var result = await _controller.CheckFileUploadSatus();

            // Assert using Fluent Assertions
            result.Should().BeOfType<JsonResult>()
          .Which.Value.Should().BeAssignableTo<object>()  
          .Which.Should().BeEquivalentTo(new { isFileUploadInProgress = true });
        }


        [Test]
        public async Task ExportSubsidiaries_WhenError_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>())).ThrowsAsync(new Exception("Some message"));

            // Act
            var result = await _controller.ExportSubsidiaries() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("SubsidiariesDownloadFailed");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task ExportSubsidiaries_WhenStreamIsNull_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockStream = new MemoryStream();
            _mockSubsidiaryService.Setup(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>())).ReturnsAsync((MemoryStream)null);

            // Act
            var result = await _controller.ExportSubsidiaries() as RedirectToActionResult;

            // Assert
            result.ActionName.Should().Be("SubsidiariesDownloadFailed");
            _mockSubsidiaryService.Verify(s => s.GetSubsidiariesStreamAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), true, It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task ConfirmRemoveSubsidiary_SelectedConfirmRemovalYes_TerminatesSubsidiaryAndRedirects()
        {
            // Arrange
            var model = new SubsidiaryConfirmRemovalViewModel
            {
                SelectedConfirmRemoval = YesNoAnswer.Yes,
                ParentOrganisationExternalId = Guid.NewGuid(),
                SubsidiaryExternalId = Guid.NewGuid(),
                SubsidiaryName = "SomeSubsidiaryToRemove"
            };

            // Act
            var result = await _controller.ConfirmRemoveSubsidiary(model);

            // Assert
            _mockSubsidiaryService.Verify(s => s.TerminateSubsidiary(
                model.ParentOrganisationExternalId, model.SubsidiaryExternalId, UserId), Times.Once);

            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(_controller.ConfirmRemoveSubsidiarySuccess));
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

        [Test]
        public async Task ConfirmRemoveSubsidiarySuccess_ReturnsSuccessfully()
        {
            // Arrange
            var expectedModel = new ConfirmRemoveSubsidiarySuccessViewModel
            {
                ReturnToSubsidiaryPage = 5,
                SubsidiaryName = DummySubsidiaryName
            };

            // Act
            var result = await _controller.ConfirmRemoveSubsidiarySuccess();

            // Assert
            result.Should().BeOfType<ViewResult>()
                .Which.Model.Should().BeOfType<ConfirmRemoveSubsidiarySuccessViewModel>()
                .Which.Should().BeEquivalentTo(expectedModel);
        }

        [Test]
        public async Task SubsidiariesList_ShouldIncludeJoinerDateAndReportingType()
        {
            // Arrange
            _mockSubsidiaryService.Setup(x => x.GetSubsidiaryFileUploadStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(SubsidiaryFileUploadStatus.NoFileUploadActive);

            // Act
            var result = await _controller.SubsidiariesList();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var viewModel = viewResult.Model.Should().BeOfType<SubsidiaryListViewModel>().Subject;

            viewModel.Organisations.Should().HaveCount(1);
            var subsidiary = viewModel.Organisations[0].Subsidiaries.First();

            subsidiary.JoinerDate.Should().Be(JoinerDate);  
            subsidiary.ReportingType.Should().Be(SelfReportingType);  
        }

        public static object[] SubsidiariesUnsuccessfulFileUploadDecisionCases() => [
            new object[] { true, "SubsidiariesList", null, new RouteValueDictionary { { "page", 1 } } },
            new object[] { false, "Get", "Landing", null }
        ];

        private List<Claim> CreateUserDataClaim(string organisationRole, string serviceRole = null)
        {
            var userData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new()
                    {
                        Id = OrganisationId,
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