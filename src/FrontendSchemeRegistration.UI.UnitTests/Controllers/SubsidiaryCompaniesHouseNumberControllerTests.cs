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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

public class SubsidiaryCompaniesHouseNumberControllerTests
{
    private const string CompaniesHouseNumber = "OE029546";
    private const string CompanyName = "EVIDEN UK INTERNATIONAL LIMITED";
    private const string NewSubsidiaryId = "123456";

    private Mock<HttpContext> _httpContextMock;
    private FrontendSchemeRegistrationSession _session;

    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ICompaniesHouseService> _companiesHouseServiceMock;

    private SubsidiaryCompaniesHouseNumberController _subsidiaryCompaniesHouseNumberController;
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

        _subsidiaryCompaniesHouseNumberController = new SubsidiaryCompaniesHouseNumberController(
            _urlOptionsMock.Object,
            _companiesHouseServiceMock.Object,
            _sessionManagerMock.Object,
            new NullLogger<SubsidiaryCompaniesHouseNumberController>());
        _subsidiaryCompaniesHouseNumberController.ControllerContext = new ControllerContext
        {
            HttpContext = new Mock<HttpContext>().Object
        };

        _subsidiaryCompaniesHouseNumberController.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_SubsidiaryCompaniesHouseNumberSearch()
    {
        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUpload}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryCompaniesHouseNumberViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch()
    {
        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryConfirmCompanyDetails");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_When_HasError()
    {
        // Arrange
        _subsidiaryCompaniesHouseNumberController.ModelState.AddModelError("file", "error");

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUpload}");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_With_CompaniesHouse_Not_Found()
    {
        // Arrange
        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(default(Company));

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUpload}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryCompaniesHouseNumberViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);

        result.ViewData.ModelState["CompaniesHouseNumber"].Should().NotBeNull();
        result.ViewData.ModelState["CompaniesHouseNumber"].Errors[0].ErrorMessage.Should().Be("CompaniesHouseNumber.NotFoundError");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_With_CompaniesHouse_Exception()
    {
        // Arrange
        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUpload}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryCompaniesHouseNumberViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);

        result.ViewData.ModelState["CompaniesHouseNumber"].Should().NotBeNull();
        result.ViewData.ModelState["CompaniesHouseNumber"].Errors[0].ErrorMessage.Should().Be("CompaniesHouseNumber.LookupFailed");
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
