using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Identity.Web;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

public class SubsidiaryCheckDetailsControllerTests
{
    private const string CompaniesHouseNumber = "0123456X";
    private const string CompanyName = "Test company";
    private const string NewSubsidiaryId = "123456";

    private Mock<HttpContext> _httpContextMock;
    private FrontendSchemeRegistrationSession _session;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ISubsidiaryService> _subsidiaryServiceMock;
    private SubsidiaryCheckDetailsController _subsidiaryCheckDetailsController;
    private Mock<IUrlHelper> _urlHelperMock;
    private Mock<ClaimsPrincipal> _userMock;
    private UserData _userData;
    private Guid _organisationId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _httpContextMock = new Mock<HttpContext>();

        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUpload);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);

        _userMock = new Mock<ClaimsPrincipal>();

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

        _subsidiaryServiceMock = new Mock<ISubsidiaryService>();
        _subsidiaryServiceMock.Setup(x => x.SaveSubsidiary(It.IsAny<SubsidiaryDto>()))
            .ReturnsAsync(NewSubsidiaryId);

        _userData = new UserData
        {
            Email = "test@test.com",
            Organisations = new List<EPR.Common.Authorization.Models.Organisation> { new() { Id = _organisationId } }
        };

        _userMock.Setup(x => x.Claims).Returns(new List<Claim>
            {
                new(ClaimTypes.UserData, JsonConvert.SerializeObject(_userData)),
                new(ClaimConstants.ObjectId, _userId.ToString())
            });

        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextMock.Setup(x => x.Session).Returns(Mock.Of<ISession>());

        _subsidiaryCheckDetailsController = new SubsidiaryCheckDetailsController(_sessionManagerMock.Object, _subsidiaryServiceMock.Object);
        _subsidiaryCheckDetailsController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Query = new QueryCollection() },
                Session = Mock.Of<ISession>(),
                User = _userMock.Object
            },
        };
        _subsidiaryCheckDetailsController.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_SubsidiaryCheckDetailsView_WhenCalled()
    {
        // Act
        var result = await _subsidiaryCheckDetailsController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCheckDetails");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryLocation}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryCheckDetailsViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryCheckDetailsViewModel;
        viewModel.CompanyName.Should().Be(CompanyName);
    }

    [Test]
    public async Task Get_SubsidiaryCheckDetailsView_With_Null_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession = null;

        // Act
        var result = await _subsidiaryCheckDetailsController.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Get_SubsidiaryCheckDetailsView_With_Null_Company_In_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession.Company = null;

        // Act
        var result = await _subsidiaryCheckDetailsController.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Post_RedirectsToSubsidiaryAdded()
    {
        // Act
        var result = await _subsidiaryCheckDetailsController.Post(new SubsidiaryCheckDetailsViewModel()) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryAdded");
    }

    [Test]
    public async Task Post_RedirectsToSubsidiaryAdded_With_Null_Session()
    {
        // Arrange
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(default(FrontendSchemeRegistrationSession));

        // Act
        var result = await _subsidiaryCheckDetailsController.Post(new SubsidiaryCheckDetailsViewModel()) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Post_RedirectsToSubsidiaryAdded_With_Null_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession = null;

        // Act
        var result = await _subsidiaryCheckDetailsController.Post(new SubsidiaryCheckDetailsViewModel()) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Post_RedirectsToSubsidiaryAdded_With_Null_Company_In_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession.Company = null;

        // Act
        var result = await _subsidiaryCheckDetailsController.Post(new SubsidiaryCheckDetailsViewModel()) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Post_SubsidiaryCheckDetailsView_When_HasModelError()
    {
        // Arrange
        _subsidiaryCheckDetailsController.ModelState.AddModelError("file", "Some error");

        // Act
        var result = await _subsidiaryCheckDetailsController.Post(new SubsidiaryCheckDetailsViewModel()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCheckDetails");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryLocation}");
    }

    [Test]
    public async Task Post_CallsService_And_Returns_NewSubsidiaryId()
    {
        // Act
        var result = await _subsidiaryCheckDetailsController.Post(new SubsidiaryCheckDetailsViewModel()) as RedirectToActionResult;

        // Assert
        _subsidiaryServiceMock.VerifyAll();

        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryAdded");

        _subsidiaryServiceMock.VerifyAll();
        _session.SubsidiarySession.Company.OrganisationId.Should().Be(NewSubsidiaryId);
    }
}
