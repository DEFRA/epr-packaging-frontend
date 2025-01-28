namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using AutoFixture;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

[TestFixture]
public class PrnsObligationControllerTests
{
    private Mock<IUrlHelper> _urlHelperMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IPrnService> _prnServiceMock;
    private Mock<IOptions<ExternalUrlOptions>> _urlOptionsMock = null;
    private PrnsObligationController _controller;
    private static readonly IFixture _fixture = new Fixture();
    private Mock<ILogger<PrnsObligationController>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _prnServiceMock = new Mock<IPrnService>();
        _prnServiceMock.Setup(prn => prn.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(new PrnListViewModel());

        _urlOptionsMock = new Mock<IOptions<ExternalUrlOptions>>();
        var externalUrlOptions = new ExternalUrlOptions
        {
            ProducerResponsibilityObligations = "https://producer-responsibility-obligations.service.gov.uk"
        };

        _urlOptionsMock!.Setup(x => x.Value).Returns(externalUrlOptions);
        var globalVariables = Options.Create(new GlobalVariables { BasePath = "BasePath", LogPrefix = "[FrontendSchemaRegistration]" });
        _loggerMock = new Mock<ILogger<PrnsObligationController>>();

        _controller = new PrnsObligationController(_sessionManagerMock.Object, _prnServiceMock.Object, globalVariables, _urlOptionsMock.Object, _loggerMock.Object)
        {
            Url = _urlHelperMock.Object
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object
            }
        };
    }

    [Test]
    public async Task ObligationsHome_Returns_View()
    {
        // Arrange
        var viewModel = _fixture.Create<PrnObligationViewModel>();
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(It.IsAny<int>())).ReturnsAsync(viewModel);

        // Act
        var result = await _controller.ObligationsHome() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.Model.Should().BeOfType<PrnObligationViewModel>();

        var model = result.Model as PrnObligationViewModel;
        model.MaterialObligationViewModels.Count.Should().BeGreaterThan(1);
        model.GlassMaterialObligationViewModels.Count.Should().BeGreaterThan(1);
        _controller.ViewData.Should().ContainKey("HomeLinkToDisplay");
    }

    [Test]
    public async Task ObligationPerMaterial_WhenGivenGlass_ReturnsView()
    {
        // Arrange
        string material = "Glass";
        int year = DateTime.Now.Year;
        PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(year)).ReturnsAsync(viewModel);

        // Act
        var response = await _controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        viewModel.GlassMaterialObligationViewModels.Count().Should().BeGreaterThan(0);
        viewModel.MaterialObligationViewModels.Count().Should().Be(0);
        _controller.ViewData["GlassOrNonGlassResource"].Should().Be("glass");
        _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
    }

    [Theory]
    [TestCase("Aluminium", "aluminium")]
    [TestCase("Paper", "paper_board_fibre")]
    [TestCase("Plastic", "plastic")]
    [TestCase("Steel", "steel")]
    [TestCase("Wood", "wood")]
    public async Task ObligationPerMaterial_WhenGivenNonGlass_ReturnsView(string material, string resource)
    {
        // Arrange
        int year = DateTime.Now.Year;
        PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        foreach (var item in viewModel.MaterialObligationViewModels)
        {
            item.MaterialName = Enum.Parse<MaterialType>(material);
            break;
        }
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(year)).ReturnsAsync(viewModel);

        // Act
        var response = await _controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        viewModel.GlassMaterialObligationViewModels.Count().Should().Be(0);
        viewModel.MaterialObligationViewModels.Count().Should().BeGreaterThan(0);
        _controller.ViewData["GlassOrNonGlassResource"].Should().Be(resource);
        _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        _prnServiceMock.Verify(x => x.GetRecyclingObligationsCalculation(year), Times.Once);

    }

    [Test]
    public async Task ObligationPerMaterial_WhenGivenUnrecognisedMaterial_ReturnsView()
    {
        // Arrange
        string material = "Unknown";
        int year = DateTime.Now.Year;
        PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(year)).ReturnsAsync(viewModel);

        // Act
        var response = await _controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        _prnServiceMock.Verify(x => x.GetRecyclingObligationsCalculation(It.IsAny<int>()), Times.Never());
        _controller.ViewData.Should().NotContainKey("GlassOrNonGlassResource");
        _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        _controller.ViewData.Should().ContainKey("ProducerResponsibilityObligationsLink");
    }

    [Theory]
    [TestCase(OrganisationRoles.Producer, 1)]
    [TestCase(OrganisationRoles.ComplianceScheme, 2)]
    [TestCase(OrganisationRoles.Producer, 3)]
    [TestCase(OrganisationRoles.ComplianceScheme, 4)]
    public async Task FillViewModelFromSessionAsync_Returns_Valid_ViewModel(string organisationRole, int nationId)
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var deadlineYear = currentYear + 1;
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
                        OrganisationRole = organisationRole,
                        Name = "Test Organisation",
                        NationId = nationId
                    }
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

        var viewModel = _fixture.Create<PrnObligationViewModel>();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear);

        // Assert
        viewModel.OrganisationRole.Should().BeEquivalentTo(organisationRole);
        viewModel.OrganisationName.Should().BeEquivalentTo("Test Organisation");
        viewModel.NationId.Should().NotBeNull();
        viewModel.NationId.Value.Should().Be(nationId);
        viewModel.CurrentYear.Should().Be(currentYear);
        viewModel.DeadlineYear.Should().Be(deadlineYear);
    }

    [Test]
    public async Task FillViewModelFromSessionAsync_WhenOrganisationIsNull_ViewModelDoesNotHaveOrganisationDetails()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = null
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

        var viewModel = new PrnObligationViewModel();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, 2024);

        // Assert
        viewModel.OrganisationRole.Should().BeNullOrEmpty();
        viewModel.OrganisationName.Should().BeNullOrEmpty();
        viewModel.NationId.Should().BeNull();
    }

    [Test]
    public async Task FillViewModelFromSessionAsync_Returns_ViewModel_With_DefaultSession_When_No_Session_Exists()
    {
        // Arrange
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession)null);

        PrnObligationViewModel viewModel = new();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, 2024);

        // Assert
        viewModel.OrganisationRole.Should().BeEquivalentTo(null);
        viewModel.OrganisationName.Should().BeEquivalentTo(null);
        viewModel.NationId.Should().BeNull(null);
        viewModel.CurrentYear.Should().Be(0);
        viewModel.DeadlineYear.Should().Be(0);
    }

}
