namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Linq.Expressions;
using System.Security.Claims;
using Application.DTOs;
using Application.DTOs.ComplianceScheme;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Application.DTOs.ComplianceSchemeMember;
using Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Sessions;
using UI.ViewModels;

public class SchemeMembershipControllerTests
{
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly string _organisationName = "Acme Org Ltd";
    private readonly string _organisationNumber = "487 951";
    private readonly Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManager = new();
    private readonly Mock<IComplianceSchemeMemberService> _complianceSchemeMemberService = new();
    private readonly Mock<IOptions<GlobalVariables>> _globalVariablesMock = new();
    private readonly Mock<IOptions<SiteDateOptions>> _siteDateOptionsMock = new();
    private readonly Mock<IOptions<ComplianceSchemeMembersPaginationOptions>> _complianceSchemeMemberPaginationOptionsMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly NullLogger<SchemeMembershipController> _nullLogger = new();
    private SchemeMembershipController _systemUnderTest;
    private UserData? _userData;
    private List<Claim>? _claims;
    private FrontendSchemeRegistrationSession? _session;

    [SetUp]
    public void SetUp()
    {
        _userData = new UserData
        {
            Id = Guid.NewGuid(),
            ServiceRole = ServiceRoles.ApprovedPerson,
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                    Name = _organisationName,
                    OrganisationRole = "ComplianceScheme",
                    OrganisationNumber = _organisationNumber
                }
            }
        };

        _claims = new List<Claim>
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(_userData))
        };

        _userMock.Setup(x => x.Claims).Returns(_claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _session = new FrontendSchemeRegistrationSession
        {
            UserData = _userData,
            SchemeMembershipSession = new SchemeMembershipSession { Journey = new List<string>() }
        };
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _globalVariablesMock.Setup(g => g.Value).Returns(new GlobalVariables { BasePath = "/" });
        _siteDateOptionsMock.Setup(g => g.Value).Returns(new SiteDateOptions { DateFormat = "d MMMM yyyy" });

        _complianceSchemeMemberPaginationOptionsMock
            .Setup(g => g.Value)
            .Returns(new ComplianceSchemeMembersPaginationOptions() { PageSize = 50 });

        _systemUnderTest = new SchemeMembershipController(
            _sessionManager.Object,
            _globalVariablesMock.Object,
            _complianceSchemeMemberService.Object,
            _nullLogger,
            _siteDateOptionsMock.Object,
            _complianceSchemeMemberPaginationOptionsMock.Object)
        {
            ControllerContext = { HttpContext = _httpContextMock.Object }
        };
    }

    [Test]
    public async Task GetComplianceSchemeMembers_WhenSchemeIsNotFound_RedirectsToLandingGet()
    {
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());
        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMembers(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((ComplianceSchemeMembershipResponse)null);

        var result = await _systemUnderTest.SchemeMembers(It.IsAny<Guid>(), string.Empty, 1) as RedirectToActionResult;

        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task GetComplianceSchemeMembers_WhenNoLinkedOrganisations_RedirectsToLandingGet()
    {
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());

        var complianceSchemeMembershipResponse = new ComplianceSchemeMembershipResponse { LinkedOrganisationCount = 0 };

        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMembers(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(complianceSchemeMembershipResponse);

        var result = await _systemUnderTest.SchemeMembers(It.IsAny<Guid>(), string.Empty, 1) as RedirectToActionResult;

        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task
        GetComplianceSchemeMembers_WhenPageGreaterThanOneAndPageGreaterThanPageCount_RedirectsToSchemeMembers()
    {
        // Arrange
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());

        var complianceSchemeMembershipResponse = new ComplianceSchemeMembershipResponse
        {
            LinkedOrganisationCount = 0,
            PagedResult = new PaginatedResponse<ComplianceSchemeMemberDto>
            {
                TotalItems = 2, PageSize = 1
            }
        };

        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMembers(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(complianceSchemeMembershipResponse);

        // Act
        var result = await _systemUnderTest.SchemeMembers(It.IsAny<Guid>(), string.Empty, 10) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(SchemeMembershipController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(SchemeMembershipController.SchemeMembers));
    }

    [Test]
    public async Task
        GetComplianceSchemeMembers_WhenPageHttpGetCalledWithSessionAsNull_ThenMemberDetailsPageViewDoesNotReturnModel()
    {
        // Arranges
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
        var selectedSchemeId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.SchemeMembers(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task
        GetComplianceSchemeMembers_WhenHttpGetCalledWithOrganisationAsNull_ThenMemberDetailsPageViewDoesNotReturnModel()
    {
        // Arranges
        _userData = new UserData
        {
            Id = Guid.NewGuid(),
            ServiceRole = "Basic User",
            Organisations = new()
            {
                null
            }
        };
        _claims = new List<Claim>
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(_userData))
        };

        _userMock.Setup(x => x.Claims).Returns(_claims);
        var selectedSchemeId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.SchemeMembers(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task GetComplianceSchemeMembers_SearchNoResults_ResetLinkSet()
    {
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());

        var complianceSchemeMembershipResponse = new ComplianceSchemeMembershipResponse
        {
            LinkedOrganisationCount = 1,
            LastUpdated = DateTime.UtcNow,
            PagedResult = new PaginatedResponse<ComplianceSchemeMemberDto> { TotalItems = 0, Items = new List<ComplianceSchemeMemberDto>() }
        };

        var id = Guid.NewGuid();
        var resetLinkText = "resetlinktext";
        var searchString = "TestSearch";

        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        Expression<Func<IUrlHelper, string>> urlSetup = url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "SchemeMembers"));
        mockUrlHelper.Setup(urlSetup).Returns(resetLinkText).Verifiable();

        _systemUnderTest.Url = mockUrlHelper.Object;

        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMembers(
                It.IsAny<Guid>(), id, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(complianceSchemeMembershipResponse);

        var result = await _systemUnderTest.SchemeMembers(id, searchString, 1) as ViewResult;

        result.ViewName.Should().Be("SchemeMembers");

        result.Model.Should().BeOfType<SchemeMembersModel>();
        var model = result.Model as SchemeMembersModel;
        model.ResetLink.Should().Be(resetLinkText);
    }

    [Test]
    public async Task GetComplianceSchemeMembers_WhenNonPositivePageIsAccessed_ThenRedirectToFirstPage()
    {
        var httpResponseMock = new Mock<HttpResponse>();

        _httpContextMock.Setup(x => x.Response).Returns(httpResponseMock.Object);

        var result = await _systemUnderTest.SchemeMembers(Guid.NewGuid(), page: -100) as RedirectToActionResult;

        result!.ActionName.Should().Be("SchemeMembers");
        result!.RouteValues!["page"].Should().Be(1);
    }

    [Test]
    public async Task GetComplianceSchemeMembers_WhenNonExistingPageIsAccessed_ThenRedirectToFirstPage()
    {
        var httpResponseMock = new Mock<HttpResponse>();
        const int pageSize = 50;
        const int totalPages = 100;

        _httpContextMock.Setup(x => x.Response).Returns(httpResponseMock.Object);

        var complianceSchemeMembershipResponse = new ComplianceSchemeMembershipResponse
        {
            PagedResult = new()
            {
                PageSize = pageSize,
                TotalItems = totalPages * pageSize
            }
        };

        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMembers(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(complianceSchemeMembershipResponse);

        var result = await _systemUnderTest.SchemeMembers(Guid.NewGuid(), page: totalPages + 1) as RedirectToActionResult;

        result!.ActionName.Should().Be("SchemeMembers");
        result!.RouteValues!["page"].Should().Be(1);
    }

    [Test]
    public async Task PostSchemeMembers_CallsGetSchemeMembers_SchemeLinkIsSent()
    {
        // Arrange
        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(Mock.Of<FrontendSchemeRegistrationSession>());

        var complianceSchemeMembershipResponse = new ComplianceSchemeMembershipResponse
        {
            LinkedOrganisationCount = 1,
            LastUpdated = DateTime.UtcNow,
            PagedResult = new PaginatedResponse<ComplianceSchemeMemberDto> { TotalItems = 0, Items = new List<ComplianceSchemeMemberDto>() }
        };

        var id = Guid.NewGuid();
        var resetLinkText = "resetlinktext";
        var searchString = "TestSearch";

        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        Expression<Func<IUrlHelper, string>> urlSetup = url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "SchemeMembers"));
        mockUrlHelper.Setup(urlSetup).Returns(resetLinkText).Verifiable();

        _systemUnderTest.Url = mockUrlHelper.Object;

        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMembers(
                It.IsAny<Guid>(), id, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(complianceSchemeMembershipResponse);

        // Act
        var result = await _systemUnderTest.SchemeMembers(id, searchString) as ViewResult;

        // Assert
        result.ViewName.Should().Be(nameof(SchemeMembershipController.SchemeMembers));
        result.Model.Should().BeOfType<SchemeMembersModel>();
        result.Model.As<SchemeMembersModel>().ResetLink.Should().Be(resetLinkText);
    }

    [Test]
    public async Task GetReasonsForRemoval_ComplianceSchemeMemberIsNull_RedirectHome()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();

        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(
            (ComplianceSchemeMemberDetails)null);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task GetReasonsForRemoval_SessionIsNull_RedirectHome()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var dto = new ComplianceSchemeMemberDetails
        {
            OrganisationNumber = _organisationNumber,
            OrganisationName = _organisationName,
            RegisteredNation = "England",
            ProducerType = "Partnership",
            CompanyHouseNumber = "12333",
            ComplianceScheme = "Co2"
        };

        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(dto);
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task PostReasonsForRemoval_ModelIsNotApprovedUser_ReturnUnauthorizedResult()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var reasonForRemovalViewModel = new ReasonForRemovalViewModel();

        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                ServiceRole = "Basic User",
            }
        };

        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, reasonForRemovalViewModel);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task PostReasonsForRemoval_ModelStateNotValidComplianceSchemeNull_RedirectToHome()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var reasonForRemovalViewModel = new ReasonForRemovalViewModel();

        _systemUnderTest.ModelState.AddModelError("test", "This is a test error");

        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                ServiceRole = "Approved Person",
            }
        };

        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((ComplianceSchemeMemberDetails)null);
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, reasonForRemovalViewModel) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task PostReasonsForRemoval_ModelStateNotValidComplianceSchemeNotNull_ReturnViewModelContainingOrganisationName()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var reasonForRemovalViewModel = new ReasonForRemovalViewModel();

        _systemUnderTest.ModelState.AddModelError("test", "This is a test error");

        var dto = new ComplianceSchemeMemberDetails
        {
            OrganisationNumber = _organisationNumber,
            OrganisationName = _organisationName,
            RegisteredNation = "England",
            ProducerType = "Partnership",
            CompanyHouseNumber = "12333",
            ComplianceScheme = "Co2"
        };

        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                ServiceRole = "Approved Person",
            }
        };

        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(dto);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, reasonForRemovalViewModel) as ViewResult;

        // Assert
        ((ReasonForRemovalViewModel)result.Model).OrganisationName.Should().Be(_organisationName);
    }

    [Test]
    public async Task PostReasonsForRemoval_ModelStateNotValidNotSelectedCodeComplianceSchemeNull_RedirectToHome()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var reasonForRemovalViewModel = new ReasonForRemovalViewModel();

        var reasonForRemoval = new ComplianceSchemeReasonsRemovalDto[]
        {
            new()
            {
                Code = "test",
            }
        };

        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                ServiceRole = "Approved Person",
            }
        };

        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((ComplianceSchemeMemberDetails)null);
        _complianceSchemeMemberService.Setup(x => x.GetReasonsForRemoval()).ReturnsAsync(reasonForRemoval);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, reasonForRemovalViewModel) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task PostReasonsForRemoval_ModelStateNotValidNotSelectedCodeComplianceSchemeNotNull_ReturnViewModelContainingOrganisationName()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var reasonForRemovalViewModel = new ReasonForRemovalViewModel();

        var dto = new ComplianceSchemeMemberDetails
        {
            OrganisationNumber = _organisationNumber,
            OrganisationName = _organisationName,
            RegisteredNation = "England",
            ProducerType = "Partnership",
            CompanyHouseNumber = "12333",
            ComplianceScheme = "Co2"
        };

        var reasonForRemoval = new ComplianceSchemeReasonsRemovalDto[]
        {
            new()
            {
                Code = "test",
            }
        };

        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                ServiceRole = "Approved Person",
            }
        };

        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(dto);
        _complianceSchemeMemberService.Setup(x => x.GetReasonsForRemoval()).ReturnsAsync(reasonForRemoval);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, reasonForRemovalViewModel) as ViewResult;

        // Assert
        result.Model.As<ReasonForRemovalViewModel>().OrganisationName.Should().Be(_organisationName);
    }

    [Test]
    public async Task GetRemovalTellUsMore_SessionNull_RedirectsToHome()
    {
        // Arrange
        var selectedComplianceScheme = Guid.NewGuid();

        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await _systemUnderTest.TellUsMore(selectedComplianceScheme) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    [TestCase(1, 10, 95, 1)]
    [TestCase(2, 10, 95, 11)]
    public void PagingDetail_PropertiesSet_FromItemShouldBeCorrectValue(int currentPage, int pageSize, int totalItems, int expectedValue)
    {
        var pagingDetail = new PagingDetail
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        pagingDetail.FromItem.Should().Be(expectedValue);
    }

    [Test]
    [TestCase(1, 10, 95, 10)]
    [TestCase(2, 10, 95, 20)]
    [TestCase(10, 10, 95, 95)]
    public void PagingDetail_PropertiesSet_ToItemShouldBeCorrectValue(int currentPage, int pageSize, int totalItems, int expectedValue)
    {
        var pagingDetail = new PagingDetail
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        pagingDetail.ToItem.Should().Be(expectedValue);
    }

    [Test]
    [TestCase(10, 95, 10)]
    [TestCase(50, 1001, 21)]
    [TestCase(0, 1001, 0)]
    public void PagingDetail_PropertiesSet_PageCountShouldBeCorrectValue(int pageSize, int totalItems, int expectedValue)
    {
        var pagingDetail = new PagingDetail
        {
            PageSize = pageSize,
            TotalItems = totalItems
        };
        pagingDetail.PageCount.Should().Be(expectedValue);
    }

    [Test]
    [TestCase(1, 10, 195, 4)]
    [TestCase(2, 10, 195, 5)]
    [TestCase(3, 10, 195, 6)]
    [TestCase(4, 10, 195, 7)]
    [TestCase(10, 10, 195, 7)]
    [TestCase(20, 10, 195, 4)]
    [TestCase(19, 10, 195, 5)]
    [TestCase(18, 10, 195, 6)]
    [TestCase(17, 10, 195, 7)]
    [TestCase(15, 10, 195, 7)]
    public void PagingDetail_PropertiesSet_PagingListShouldBeCorrectValue(int currentPage, int pageSize, int totalItems,
        int expectedLength)
    {
        var pagingDetail = new PagingDetail
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        pagingDetail.PagingList.Should().HaveCount(expectedLength);
    }

    [Test]
    [TestCase(1, 10, 195, 2)]
    [TestCase(2, 10, 195, 3)]
    [TestCase(3, 10, 195, 4)]
    [TestCase(4, 10, 195, 5)]
    [TestCase(10, 10, 195, 11)]
    [TestCase(20, 10, 195, 0)]
    [TestCase(19, 10, 195, 20)]
    [TestCase(18, 10, 195, 19)]
    [TestCase(17, 10, 195, 18)]
    [TestCase(15, 10, 195, 16)]
    public void PagingDetails_PropertiesSet_NextPageShouldBeCorrectValue(int currentPage, int pageSize, int totalItems,
        int expectedPage)
    {
        var pagingDetail = new PagingDetail
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        pagingDetail.NextPage.Should().Be(expectedPage);
    }

    [Test]
    [TestCase(1, 0)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(10, 9)]
    [TestCase(20, 19)]
    [TestCase(19, 18)]
    [TestCase(18, 17)]
    [TestCase(17, 16)]
    [TestCase(15, 14)]
    public void PagingDetails_PropertiesSet_PreviousPageShouldBeCorrectValue(int currentPage, int expectedPage)
    {
        var pagingDetail = new PagingDetail
        {
            CurrentPage = currentPage
        };

        pagingDetail.PreviousPage.Should().Be(expectedPage);
    }

    [Test]
    public async Task
        GivenOnMemberDetailsPage_WhenMemberDetailsPageHttpGetCalled_ThenMemberDetailsPageViewModelReturned()
    {
        // Arranges
        var selectedSchemeId = Guid.NewGuid();
        var dto = new ComplianceSchemeMemberDetails
        {
            OrganisationNumber = _organisationNumber,
            OrganisationName = _organisationName,
            RegisteredNation = "England",
            ProducerType = "Partnership",
            CompanyHouseNumber = "12333",
            ComplianceScheme = "Co2"
        };

        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(dto);

        // Act
        var result = await _systemUnderTest.MemberDetails(selectedSchemeId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<MemberDetailsViewModel>();

        var viewModel = (result as ViewResult).Model as MemberDetailsViewModel;
        viewModel.OrganisationNumber.Should().Be(_organisationNumber);
        viewModel.OrganisationName.Should().Be(_organisationName);
    }

    [Test]
    public async Task
        GivenOnMemberDetailsPage_WhenMemberDetailsPageHttpGetCalledWithNull_ThenMemberDetailsPageViewDoesNotReturnModel()
    {
        // Arranges
        var selectedSchemeId = Guid.NewGuid();

        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((ComplianceSchemeMemberDetails)null);

        // Act
        var result = await _systemUnderTest.MemberDetails(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task ConfirmRemoval_WhenConfirmRemovalPageHttpGetCalled_ThenConfirmRemovalViewModelReturned()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();

        var dto = new ComplianceSchemeMemberDetails
        {
            OrganisationNumber = _organisationNumber,
            OrganisationName = _organisationName,
            RegisteredNation = "England",
            ProducerType = "Partnership",
            CompanyHouseNumber = "12333",
            ComplianceScheme = "Co2"
        };

        _complianceSchemeMemberService.Setup(x => x.GetComplianceSchemeMemberDetails(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(dto);

        _systemUnderTest = new SchemeMembershipController(
            _sessionManager.Object,
            _globalVariablesMock.Object,
            _complianceSchemeMemberService.Object,
            _nullLogger,
            _siteDateOptionsMock.Object,
            _complianceSchemeMemberPaginationOptionsMock.Object)
        {
            ControllerContext =
            {
                HttpContext = new DefaultHttpContext
                {
                    Request =
                    {
                        Query = new QueryCollection(new Dictionary<string, StringValues>
                        {
                            { "SelectedReasonForRemoval", new StringValues(selectedSchemeId.ToString()) }
                        })
                    },
                    Session = new Mock<ISession>().Object,
                    User = _userMock.Object
                }
            }
        };

        // Act
        var result = await _systemUnderTest.ConfirmRemoval(selectedSchemeId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<ConfirmRemovalViewModel>();

        var viewModel = (result as ViewResult).Model as ConfirmRemovalViewModel;
        viewModel.OrganisationName.Should().Be(_organisationName);
    }

    [Test]
    public async Task ConfirmRemoval_WhenConfirmRemovalPageHttpPostCalledWithValidModelStateWithYesResponse_ThenRedirectWithCorrectPageAndData()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var viewModel = new ConfirmRemovalViewModel
        {
            OrganisationName = _organisationName,
            SelectedConfirmRemoval = YesNoAnswer.Yes
        };

        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>());
        tempData["OrganisationName"] = "Biffpack";

        const string selectedReasonForRemoval = "A";

        _sessionManager
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                UserData = _userData,
                SchemeMembershipSession = new SchemeMembershipSession
                {
                    Journey = new List<string>(),
                    SelectedReasonForRemoval = selectedReasonForRemoval
                },
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto
                    {
                        Id = complianceSchemeId
                    }
                }
            });

        _systemUnderTest = new SchemeMembershipController(
            _sessionManager.Object,
            _globalVariablesMock.Object,
            _complianceSchemeMemberService.Object,
            _nullLogger,
            _siteDateOptionsMock.Object,
            _complianceSchemeMemberPaginationOptionsMock.Object)
        {
            ControllerContext =
            {
                HttpContext = _httpContextMock.Object
            },
            TempData = tempData
        };

        _complianceSchemeMemberService
            .Setup(c => c.RemoveComplianceSchemeMember(_organisationId, complianceSchemeId, selectedSchemeId, selectedReasonForRemoval, null)).ReturnsAsync(
                new RemovedComplianceSchemeMember
                {
                    OrganisationName = "Biffpack"
                });
        // Act
        var result = await _systemUnderTest.ConfirmRemoval(selectedSchemeId, viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        _complianceSchemeMemberService.Verify(
            service => service.RemoveComplianceSchemeMember(_organisationId, complianceSchemeId, selectedSchemeId, selectedReasonForRemoval, null),
            Times.Once);
    }

    [Test]
    public async Task ConfirmRemoval_WhenConfirmRemovalPageHttpPostCalledWithValidModelStateWithNoResponse_ThenRedirectWithCorrectPageAndData()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var viewModel = new ConfirmRemovalViewModel
        {
            OrganisationName = _organisationName,
            SelectedConfirmRemoval = YesNoAnswer.No
        };
        // Act
        var result = await _systemUnderTest.ConfirmRemoval(selectedSchemeId, viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();

        _complianceSchemeMemberService.Verify(
            service => service.RemoveComplianceSchemeMember(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task ConfirmRemoval_WhenConfirmRemovalPageHttpGetCalledWithBasicUser_ThenRedirectToUnAuthorisedPage()
    {
        // Arrange
        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                }
            },
            ServiceRole = ServiceRoles.BasicUser
        };

        var claims = new List<Claim>()
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(userData))
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        var session = new FrontendSchemeRegistrationSession() { UserData = userData, SchemeMembershipSession = new SchemeMembershipSession { Journey = new List<string>() } };
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _systemUnderTest = new SchemeMembershipController(
            _sessionManager.Object,
            _globalVariablesMock.Object,
            _complianceSchemeMemberService.Object,
            _nullLogger,
            _siteDateOptionsMock.Object,
            _complianceSchemeMemberPaginationOptionsMock.Object)
        {
            ControllerContext =
            {
                HttpContext = _httpContextMock.Object
            }
        };

        var selectedSchemeId = Guid.NewGuid();
        // Act
        var result = await _systemUnderTest.ConfirmRemoval(selectedSchemeId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        _complianceSchemeMemberService.Verify(
            service => service.RemoveComplianceSchemeMember(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Test]
    public async Task ConfirmationOfRemoval_WhenConfirmationOfRemovalPageCalled_ThenConfirmRemovalViewModelReturned()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>());

        var session = new FrontendSchemeRegistrationSession()
        {
            UserData = _userData,
            SchemeMembershipSession = new SchemeMembershipSession
            {
                Journey = new List<string> { $"report-data/scheme-members/{selectedSchemeId}?search=veolia&page=1" },
                RemovedSchemeMember = "Biffpack"
            },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Id = complianceSchemeId
                }
            }
        };
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        _systemUnderTest = new SchemeMembershipController(
            _sessionManager.Object,
            _globalVariablesMock.Object,
            _complianceSchemeMemberService.Object,
            _nullLogger,
            _siteDateOptionsMock.Object,
            _complianceSchemeMemberPaginationOptionsMock.Object)
        {
            ControllerContext =
            {
                HttpContext = _httpContextMock.Object
            },
            TempData = tempData
        };

        // Act
        var result = await _systemUnderTest.ConfirmationOfRemoval(selectedSchemeId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<ConfirmationOfRemovalViewModel>();

        var viewModel = (result as ViewResult).Model as ConfirmationOfRemovalViewModel;
        viewModel.OrganisationName.Should().Be("Biffpack");
        viewModel.CurrentComplianceSchemeId.Should().Be(complianceSchemeId);
    }

    [Test]
    public async Task ConfirmationOfRemoval_WhenConfirmationOfRemovalPageCalledByBasicUser_ThenRedirectsToUnauthorisedPage()
    {
        // Arrange
        var selectedSchemeId = Guid.NewGuid();
        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                }
            },
            ServiceRole = ServiceRoles.BasicUser
        };

        var claims = new List<Claim>()
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(userData))
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        var session = new FrontendSchemeRegistrationSession() { UserData = userData, SchemeMembershipSession = new SchemeMembershipSession { Journey = new List<string>() } };
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        _systemUnderTest = new SchemeMembershipController(
            _sessionManager.Object,
            _globalVariablesMock.Object,
            _complianceSchemeMemberService.Object,
            _nullLogger,
            _siteDateOptionsMock.Object,
            _complianceSchemeMemberPaginationOptionsMock.Object)
        {
            ControllerContext =
            {
                HttpContext = _httpContextMock.Object
            }
        };
        // Act
        var result = await _systemUnderTest.ConfirmationOfRemoval(selectedSchemeId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task
        GivenOnMemberDetailsPage_WhenMemberDetailsPageHttpGetCalledWithNullSession_ThenMemberDetailsPageViewDoesNotReturnModel()
    {
        // Arranges
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
        var selectedSchemeId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.MemberDetails(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }

    [Test]
    public async Task
        GivenOnMemberDetailsPage_WhenMemberDetailsPageHttpGetCalledWithNullOrganisation_ThenMemberDetailsPageViewDoesNotReturnModel()
    {
        // Arranges
        _userData = new UserData
        {
            Id = Guid.NewGuid(),
            ServiceRole = "Basic User",
            Organisations = new()
            {
                null
            }
        };
        _claims = new List<Claim>
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(_userData))
        };

        _userMock.Setup(x => x.Claims).Returns(_claims);
        var selectedSchemeId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.MemberDetails(selectedSchemeId) as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(nameof(LandingController).RemoveControllerFromName());
        result.ActionName.Should().Be(nameof(LandingController.Get));
    }
}
