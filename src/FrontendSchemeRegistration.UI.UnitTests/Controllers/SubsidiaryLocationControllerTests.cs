using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

public class SubsidiaryLocationControllerTests
{
    private const string CompaniesHouseNumber = "OE029546";
    private const string CompanyName = "EVIDEN UK INTERNATIONAL LIMITED";

    private const string SubmissionPeriod = "submissionPeriod";
    private FrontendSchemeRegistrationSession _session;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private SubsidiaryLocationController _subsidiaryLocationController;
    private Mock<IUrlHelper> _urlHelperMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.SubsidiaryConfirmCompanyDetails);
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
                SubmissionPeriod = SubmissionPeriod,
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

        _subsidiaryLocationController = new SubsidiaryLocationController(_sessionManagerMock.Object);
        _subsidiaryLocationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Query = new QueryCollection() },
                Session = Mock.Of<ISession>()
            },
        };
        _subsidiaryLocationController.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_SubsidiaryLocationView_WhenCalled()
    {
        // Act
        var result = await _subsidiaryLocationController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryLocation");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryConfirmCompanyDetails}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryLocationViewModel>();
        var model = (SubsidiaryLocationViewModel)result.ViewData.Model;
        model.UkNation.Should().BeNull();
    }

    [Test]
    public async Task Get_SubsidiaryLocationView_WhenCalled_With_Nation()
    {
        // Arrange
        _session.SubsidiarySession.UkNation = Nation.Wales;

        // Act
        var result = await _subsidiaryLocationController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryLocation");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryConfirmCompanyDetails}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryLocationViewModel>();
        var model = (SubsidiaryLocationViewModel)result.ViewData.Model;
        model.UkNation.Should().Be(Nation.Wales);
    }

    [Test]
    public async Task Get_SubsidiaryLocationView_WhenCalled_With_Null_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession = null;

        // Act
        var result = await _subsidiaryLocationController.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryLocation");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryConfirmCompanyDetails}");

        (result as ViewResult).Model.Should().BeOfType<SubsidiaryLocationViewModel>();
        var model = (SubsidiaryLocationViewModel)result.ViewData.Model;
        model.UkNation.Should().BeNull();
    }

    [Test]
    public async Task Post_RedirectsToSubsidiaryCheckDetails_WhenUkNation_Selected()
    {
        // Act
        var result = await _subsidiaryLocationController.Post(new SubsidiaryLocationViewModel { UkNation = Nation.England }) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCheckDetails");
    }

    [Test]
    public async Task Post_SubsidiaryLocationView_WhenUkNation_NotSelected()
    {
        // Arrange
        _subsidiaryLocationController.ModelState.AddModelError("file", "Some error");

        // Act
        var result = await _subsidiaryLocationController.Post(new SubsidiaryLocationViewModel()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryLocation");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryConfirmCompanyDetails}");
    }
}
