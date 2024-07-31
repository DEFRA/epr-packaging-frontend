using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

public class SubsidiaryConfirmCompanyDetailsControllerTests
{
    private const string CompaniesHouseNumber = "OE029546";
    private const string CompanyName = "EVIDEN UK INTERNATIONAL LIMITED";
    private const string NewSubsidiaryId = "123456";

    private Mock<HttpContext> _httpContextMock;
    private FrontendSchemeRegistrationSession _session;

    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ICompaniesHouseService> _companiesHouseServiceMock;

    private SubsidiaryConfirmCompanyDetailsController _subsidiaryConfirmCompanyDetailsController;
    private Mock<IOptions<ExternalUrlOptions>> _urlOptionsMock = null;

    private Mock<IUrlHelper> _urlHelperMock;
    private UserData _userData;
    private Guid _organisationId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _urlOptionsMock = new Mock<IOptions<ExternalUrlOptions>>();
        _httpContextMock = new Mock<HttpContext>();

        SetUpConfigOption();

        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUpload);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);

        _session = new FrontendSchemeRegistrationSession
        {
            SubsidiarySession = new()
            {
                Company = new Company
                {
                    CompaniesHouseNumber = CompaniesHouseNumber,
                    Name = CompanyName
                }
            },
            RegistrationSession = new()
            {
                Journey = new List<string> { PagePaths.FileUploadSubLanding }
            },
            UserData = new UserData
            {
                Organisations = new List<EPR.Common.Authorization.Models.Organisation>
                    {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                    }
            }
        };

        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(_session);
        _sessionManagerMock.Setup(x => x.UpdateSessionAsync(
            It.IsAny<ISession>(), It.IsAny<Action<FrontendSchemeRegistrationSession>>()))
            .Callback<ISession, Action<FrontendSchemeRegistrationSession>>((_, action) => action.Invoke(_session));

        _companiesHouseServiceMock = new Mock<ICompaniesHouseService>();
        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(CompaniesHouseNumber))
            .ReturnsAsync(new Company());

        _userData = new UserData
        {
            Email = "test@test.com",
            Organisations = new List<EPR.Common.Authorization.Models.Organisation> { new() { Id = _organisationId } }
        };

        _httpContextMock.Setup(x => x.Session).Returns(Mock.Of<ISession>());

        _subsidiaryConfirmCompanyDetailsController = new SubsidiaryConfirmCompanyDetailsController(
            _sessionManagerMock.Object);
        _subsidiaryConfirmCompanyDetailsController.ControllerContext = new ControllerContext
        {
            HttpContext = new Mock<HttpContext>().Object
        };

        _subsidiaryConfirmCompanyDetailsController.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_SubsidiaryConfirmCompanyDetails()
    {
        // Act
        var result = await _subsidiaryConfirmCompanyDetailsController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryConfirmCompanyDetails");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryCompaniesHouseNumberSearch}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryConfirmCompanyDetailsViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryConfirmCompanyDetailsViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);
    }

    [Test]
    public async Task Get_SubsidiaryConfirmCompanyDetails_With_Null_Session()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(default(FrontendSchemeRegistrationSession));

        // Act
        var result = await _subsidiaryConfirmCompanyDetailsController.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Get_SubsidiaryConfirmCompanyDetails_With_Null_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession = null;

        // Act
        var result = await _subsidiaryConfirmCompanyDetailsController.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Post_SubsidiaryConfirmCompanyDetails()
    {
        // Act
        var result = await _subsidiaryConfirmCompanyDetailsController.Post(new SubsidiaryConfirmCompanyDetailsViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryLocation");
    }

    [Test]
    public async Task Post_SubsidiaryConfirmCompanyDetails_With_InvalidModelState()
    {
        // Act
        _subsidiaryConfirmCompanyDetailsController
            .ModelState
            .AddModelError(nameof(SubsidiaryConfirmCompanyDetailsViewModel.CompaniesHouseNumber), "CompaniesHouseNumber.Error");

        var result = await _subsidiaryConfirmCompanyDetailsController.Post(new SubsidiaryConfirmCompanyDetailsViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryConfirmCompanyDetails");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryCompaniesHouseNumberSearch}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryConfirmCompanyDetailsViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryConfirmCompanyDetailsViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);
    }

    private void SetUpConfigOption()
    {
        var externalUrlOptions = new ExternalUrlOptions
        {
            FindAndUpdateCompanyInformation = "https://find-and-update.company-information.service.gov.uk",
            PrivacyDataProtectionPublicRegister = "url2",
            PrivacyDefrasPersonalInformationCharter = "url6",
            PrivacyInformationCommissioner = "url7",
            PrivacyEnvironmentAgency = "url8",
            PrivacyNationalResourcesWales = "url9",
            PrivacyNorthernIrelandEnvironmentAgency = "url10",
            PrivacyScottishEnvironmentalProtectionAgency = "url11"
        };

        _urlOptionsMock!
        .Setup(x => x.Value)
        .Returns(externalUrlOptions);
    }
}
