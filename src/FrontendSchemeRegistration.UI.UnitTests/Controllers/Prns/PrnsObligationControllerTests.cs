namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using AutoFixture;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
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
        var globalVariables = Options.Create(new GlobalVariables { BasePath = "BasePath", LogPrefix = "[FrontendSchemaRegistration]", OverrideCurrentYear = 2025, OverrideCurrentMonth = 11 });
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
		var organisationId = Guid.NewGuid();
		var childOrganisationId = Guid.NewGuid();

		var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
						Id = organisationId,
						OrganisationRole = OrganisationRoles.Producer,
                        Name = "Test Organisation",
                        NationId = 1
                    }
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

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
		var organisationId = Guid.NewGuid();

		var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
						Id = organisationId,
						OrganisationRole = OrganisationRoles.Producer,
                        Name = "Test Organisation",
                        NationId = 1
                    }
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        string material = "Glass";
        int month = DateTime.Now.Month;
		int year = month > 1 ? DateTime.Now.Year : DateTime.Now.Year - 1;
        
		PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(year)).ReturnsAsync(viewModel);

        // Act
        var response = await _controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        viewModel.GlassMaterialObligationViewModels.Count.Should().BeGreaterThan(0);
        viewModel.MaterialObligationViewModels.Count.Should().Be(0);
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
		var organisationId = Guid.NewGuid();
		var childOrganisationId = Guid.NewGuid();

		var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
                        Id = organisationId,
                        OrganisationRole = OrganisationRoles.Producer,
                        Name = "Test Organisation",
                        NationId = 1
                    }
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        //_prnServiceMock.Setup(p => p.GetChildOrganisationExternalIdsAsync(organisationId, null)).ReturnsAsync([childOrganisationId]);

        int year = DateTime.Now.Year;
        PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        foreach (var item in viewModel.MaterialObligationViewModels)
        {
            item.MaterialName = Enum.Parse<MaterialType>(material);
            break;
        }
		_prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(year)).ReturnsAsync(viewModel);

        var globalVariables = Options.Create(new GlobalVariables { BasePath = "BasePath", LogPrefix = "[FrontendSchemaRegistration]", OverrideCurrentYear = year, OverrideCurrentMonth = 3 });

        var controller = new PrnsObligationController(_sessionManagerMock.Object, _prnServiceMock.Object, globalVariables, _urlOptionsMock.Object, _loggerMock.Object)
        {
            Url = _urlHelperMock.Object,
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new Mock<ISession>().Object
                }
            }
        };


        // Act
        var response = await controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        viewModel.GlassMaterialObligationViewModels.Count.Should().Be(0);
        viewModel.MaterialObligationViewModels.Count.Should().BeGreaterThan(0);
        controller.ViewData["GlassOrNonGlassResource"].Should().Be(resource);
        controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        _prnServiceMock.Verify(x => x.GetRecyclingObligationsCalculation(year), Times.Once);
    }

    [Test]
    public async Task ObligationPerMaterial_WhenGivenUnrecognisedMaterial_ReturnsView()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
						Id = Guid.NewGuid(),
						OrganisationRole = OrganisationRoles.Producer,
                        Name = "Test Organisation",
                        NationId = 1
                    }
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
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

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var viewModel = new PrnObligationViewModel();
        var currentMonth = 11;

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, 2024, currentMonth);

        // Assert
        viewModel.OrganisationRole.Should().BeNullOrEmpty();
        viewModel.OrganisationName.Should().BeNullOrEmpty();
        viewModel.NationId.Should().BeNull();
    }

    [Theory]
    [TestCase(1)]
    [TestCase(3)]
    public async Task FillViewModelFromSessionAsync_Returns_Valid_ViewModelForDirectRegistrant(int nationId)
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var currentMonth = 1; // January
        var expectedComplianceYear = currentYear - 1;
        var expectedDeadlineYear = currentYear;
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
                        OrganisationRole = OrganisationRoles.Producer,
                        Name = "Test Organisation",
                        NationId = nationId
                    }
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var viewModel = _fixture.Create<PrnObligationViewModel>();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear, currentMonth);

        // Assert
        viewModel.OrganisationRole.Should().BeEquivalentTo(OrganisationRoles.Producer);
        viewModel.OrganisationName.Should().BeEquivalentTo("Test Organisation");
        viewModel.NationId.Should().NotBeNull();
        viewModel.NationId.Value.Should().Be(nationId);
        viewModel.ComplianceYear.Should().Be(expectedComplianceYear);
        viewModel.DeadlineYear.Should().Be(expectedDeadlineYear);
    }

    [Theory]
    [TestCase(2)]
    [TestCase(4)]
    public async Task FillViewModelFromSessionAsync_Returns_Valid_ViewModelForComplianceSchemeMember(int nationId)
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var currentMonth = 2; // Febuary
        var deadlineYear = currentYear + 1;
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
                        OrganisationRole = OrganisationRoles.ComplianceScheme,
                        Name = "Test Organisation",
                        NationId = nationId
                    }
                }
            },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Name = "Test Organisation",
                    NationId = nationId
                }
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var viewModel = _fixture.Create<PrnObligationViewModel>();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear, currentMonth);

        // Assert
        viewModel.OrganisationRole.Should().BeEquivalentTo(OrganisationRoles.ComplianceScheme);
        viewModel.OrganisationName.Should().BeEquivalentTo("Test Organisation");
        viewModel.NationId.Should().NotBeNull();
        viewModel.NationId.Value.Should().Be(nationId);
        viewModel.ComplianceYear.Should().Be(currentYear);
        viewModel.DeadlineYear.Should().Be(deadlineYear);
    }

    [Test]
    public async Task FillViewModelFromSessionAsync_Returns_ViewModelForComplianceSchemeMember_WhenSelectedCSIsNull()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var currentMonth = 1; // January
        var expectedComplianceYear = currentYear - 1;
        var expectedDeadlineYear = currentYear;

        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
                        OrganisationRole = OrganisationRoles.ComplianceScheme,
                        Name = "Test Organisation"
                    }
                }
            },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = null
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var viewModel = _fixture.Create<PrnObligationViewModel>();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear, currentMonth);

        // Assert
        viewModel.OrganisationName.Should().BeNull();
        viewModel.NationId.Should().Be(0);
        viewModel.ComplianceYear.Should().Be(expectedComplianceYear);
        viewModel.DeadlineYear.Should().Be(expectedDeadlineYear);
    }

    [Test]
    [Theory]
    [TestCase(2026, 1, 2025, 2026)]
    [TestCase(2025, 1, 2024, 2025)]
    [TestCase(2024, 1, 2023, 2024)]
    [TestCase(2026, 10, 2026, 2027)]
    [TestCase(2025, 11, 2025, 2026)]
    [TestCase(2024, 9, 2024, 2025)]
    public async Task FillViewModelFromSessionAsync_Returns_ComplianceYear_And_DeadlineYear_AsExpected(int currentYear, int currentMonth, int expectedComplianceYear, int expectedDeadlineYear)
    {
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
                {
                    new() {
                        OrganisationRole = OrganisationRoles.ComplianceScheme,
                        Name = "Test Organisation"
                    }
                }
            },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = null
            }
        };
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var viewModel = _fixture.Create<PrnObligationViewModel>();

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear, currentMonth);

        // Assert
        viewModel.OrganisationName.Should().BeNull();
        viewModel.NationId.Should().Be(0);
        viewModel.ComplianceYear.Should().Be(expectedComplianceYear);
        viewModel.DeadlineYear.Should().Be(expectedDeadlineYear);
    }

    [Test]    
    [TestCase(1, 2025, 2024)]
    [TestCase(null, null, null)]
    public async Task ObligationPerMaterial_ReturnsView_When_ComplianceMonth_isJanuary(int? currentMonth, int? currentYear, int? expectedComplianceYear)
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = new List<Organisation>
            {
                new() {
                    Id = Guid.NewGuid(),
                    OrganisationRole = OrganisationRoles.Producer,
                    Name = "Test Organisation",
                    NationId = 1
                }
            }
            }
        };

        if (currentMonth is null && currentYear is null)
        {
            expectedComplianceYear = DateTime.Now.Month == 1 ? DateTime.Now.Year - 1 : DateTime.Now.Year;
        }

        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        
        string material = "Paper";
        PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        foreach (var item in viewModel.MaterialObligationViewModels)
        {
            item.MaterialName = Enum.Parse<MaterialType>(material);
            break;
        }
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(expectedComplianceYear.Value)).ReturnsAsync(viewModel);

        var globalVariables = Options.Create(new GlobalVariables { BasePath = "BasePath", LogPrefix = "[FrontendSchemaRegistration]", OverrideCurrentMonth = currentMonth, OverrideCurrentYear = currentYear });
        var controller = new PrnsObligationController(_sessionManagerMock.Object, _prnServiceMock.Object, globalVariables, _urlOptionsMock.Object, _loggerMock.Object)
        {
            Url = _urlHelperMock.Object,
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new Mock<ISession>().Object
                }
            }
        };

        // Act
        var response = await controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        _prnServiceMock.Verify(x => x.GetRecyclingObligationsCalculation(It.IsAny<int>()), Times.Once());
        controller.ViewData.Should().ContainKey("GlassOrNonGlassResource");
        controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        controller.ViewData.Should().ContainKey("ProducerResponsibilityObligationsLink");
    }
}
