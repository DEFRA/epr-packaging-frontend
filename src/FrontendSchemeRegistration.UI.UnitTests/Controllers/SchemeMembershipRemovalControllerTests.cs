namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Options;
using System.Threading.Tasks;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.ComplianceSchemeMember;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using UI.Controllers;
using UI.Sessions;

public class SchemeMembershipRemovalControllerTests
{
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly string _organisationName = "Beyondly";
    private readonly string _organisationNumber = "555 111";
    private readonly Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManager = new();
    private readonly Mock<IOptions<GlobalVariables>> _globalVariablesMock = new();
    private readonly Mock<IOptions<SiteDateOptions>> _siteDateOptionsMock = new();
    private readonly Mock<IComplianceSchemeService> _complianceSchemeService = new();
    private readonly Mock<IComplianceSchemeMemberService> _complianceSchemeMemberService = new();
    private readonly NullLogger<SchemeMembershipController> _nullLogger = new();
    private readonly Mock<IOptions<ComplianceSchemeMembersPaginationOptions>> _complianceSchemeMemberPaginationOptionsMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private SchemeMembershipController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        var userData = new EPR.Common.Authorization.Models.UserData()
        {
            Id = _organisationId,
            Organisations = new()
            {
                new()
                {
                    Id = _organisationId,
                    Name = _organisationName
                }
            },
            ServiceRole = ServiceRoles.ApprovedPerson
        };

        var claims = new List<Claim>()
        {
            new(ClaimTypes.UserData, Newtonsoft.Json.JsonConvert.SerializeObject(userData))
        };

        var session = new FrontendSchemeRegistrationSession() { UserData = userData, SchemeMembershipSession = new SchemeMembershipSession { Journey = new List<string>() } };
        _sessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);
        _globalVariablesMock.Setup(g => g.Value).Returns(new GlobalVariables() { BasePath = "/" });
        _complianceSchemeMemberPaginationOptionsMock
            .Setup(g => g.Value)
            .Returns(new ComplianceSchemeMembersPaginationOptions { PageSize = 50 });
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
    }

    [Test]
    public async Task ReasonsForRemoval_WhenReasonsForRemovalPageHttpGetCalled_ThenReasonsRemovalPageReturnedwithReasons()
    {
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

        var reasonForRemoval = new List<ComplianceSchemeReasonsRemovalDto>
        {
            new()
            {
                Code = "A",
                RequiresReason = false
            },
            new()
            {
                Code = "B",
                RequiresReason = false
            },
            new()
            {
                Code = "E",
                RequiresReason = true
            }
        };

        _complianceSchemeMemberService.Setup(x => x.GetReasonsForRemoval()).ReturnsAsync(reasonForRemoval);

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<ReasonForRemovalViewModel>();
        var viewModel = (result as ViewResult).Model as ReasonForRemovalViewModel;
        viewModel.OrganisationName.Should().Be(_organisationName);
    }

    [Test]
    public async Task ReasonsForRemoval_WhenRemovalPageHttpPostCalledWithSelectionCodeE_ThenRedirectToTellUsMorePage()
    {
        var codeE = "E";
        var selectedSchemeId = Guid.NewGuid();

        var reasonForRemoval = new List<ComplianceSchemeReasonsRemovalDto>
        {
            new()
            {
                Code = "A",
                RequiresReason = false
            },
            new()
            {
                Code = "B",
                RequiresReason = false
            },
            new()
            {
                Code = "E",
                RequiresReason = true
            }
        };

        _complianceSchemeMemberService.Setup(x => x.GetReasonsForRemoval()).ReturnsAsync(reasonForRemoval);

        var viewModel = new ReasonForRemovalViewModel
        {
            OrganisationName = _organisationName,
            SelectedReasonForRemoval = codeE,
            ReasonForRemoval = reasonForRemoval
        };

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Test]
    public async Task ReasonsForRemoval_WhenRemovalPageHttpPostCalledWithSelectionCodeJ_ThenRedirectToTellUsMorePage()
    {
        var codeJ = "J";
        var selectedSchemeId = Guid.NewGuid();

        var reasonForRemoval = new List<ComplianceSchemeReasonsRemovalDto>
        {
            new()
            {
                Code = "A",
                RequiresReason = false
            },
            new()
            {
                Code = "B",
                RequiresReason = false
            },
            new()
            {
                Code = "J",
                RequiresReason = true
            }
        };

        _complianceSchemeMemberService.Setup(x => x.GetReasonsForRemoval()).ReturnsAsync(reasonForRemoval);

        var viewModel = new ReasonForRemovalViewModel
        {
            OrganisationName = _organisationName,
            SelectedReasonForRemoval = codeJ,
            ReasonForRemoval = reasonForRemoval
        };

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Test]
    public async Task ReasonsForRemoval_WhenRemovalPageHttpPostCalledWithSelection_ThenRedirectToConfirmRemovalPage()
    {
        var codeA = "A";
        var selectedSchemeId = Guid.NewGuid();

        var reasonForRemoval = new List<ComplianceSchemeReasonsRemovalDto>
        {
            new()
            {
                Code = "A",
                RequiresReason = false
            },
            new()
            {
                Code = "B",
                RequiresReason = false
            }
        };

        _complianceSchemeMemberService.Setup(x => x.GetReasonsForRemoval()).ReturnsAsync(reasonForRemoval);

        var viewModel = new ReasonForRemovalViewModel
        {
            OrganisationName = _organisationName,
            SelectedReasonForRemoval = codeA,
            ReasonForRemoval = reasonForRemoval
        };

        // Act
        var result = await _systemUnderTest.ReasonsForRemoval(selectedSchemeId, viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Test]
    public async Task RemovalTellUsMore_WhenTellUsMorePageHttpGetCalled_ThenRemovalTellUsMoreViewModelReturned()
    {
        var selectedSchemeId = Guid.NewGuid();

        // Act
        var result = await _systemUnderTest.TellUsMore(selectedSchemeId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<RemovalTellUsMoreViewModel>();
    }

    [Test]
    public async Task RemovalTellUsMore_PageIsSavedWithNoAnswer_ReturnsViewWithError()
    {
        var selectedSchemeId = Guid.NewGuid();
        _systemUnderTest.ModelState.AddModelError(nameof(RemovalTellUsMoreViewModel.TellUsMore), "More details");

        // Act
        var result = await _systemUnderTest.TellUsMore(selectedSchemeId, new RemovalTellUsMoreViewModel());

        // Assert
        result.Should().BeOfType<ViewResult>();

        var viewResult = (ViewResult)result;

        viewResult.Model.Should().BeOfType<RemovalTellUsMoreViewModel>();
    }
}
