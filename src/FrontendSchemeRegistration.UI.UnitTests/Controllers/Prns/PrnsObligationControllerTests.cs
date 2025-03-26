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
		var organisationId = Guid.NewGuid();
		var childOrganisationId = Guid.NewGuid();
		var externalIdsIcludingProducerExternalId = new List<Guid> { organisationId, childOrganisationId };

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
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);

		_prnServiceMock.Setup(p => p.GetChildOrganisationExternalIdsAsync(organisationId, null)).ReturnsAsync([childOrganisationId]);

		var viewModel = _fixture.Create<PrnObligationViewModel>();
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(externalIdsIcludingProducerExternalId, It.IsAny<int>())).ReturnsAsync(viewModel);

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
		var childOrganisationId = Guid.NewGuid();
		var externalIdsIcludingProducerExternalId = new List<Guid> { organisationId, childOrganisationId };

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
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);
        string material = "Glass";

		_prnServiceMock.Setup(x => x.GetChildOrganisationExternalIdsAsync(organisationId, null)).ReturnsAsync([childOrganisationId]);

		int year = DateTime.Now.Year;
		PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(externalIdsIcludingProducerExternalId, year)).ReturnsAsync(viewModel);

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
		var externalIdsIcludingProducerExternalId = new List<Guid> { organisationId, childOrganisationId };

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

		_prnServiceMock.Setup(p => p.GetChildOrganisationExternalIdsAsync(organisationId, null)).ReturnsAsync([childOrganisationId]);

		int year = DateTime.Now.Year;
        PrnObligationViewModel viewModel = _fixture.Create<PrnObligationViewModel>();
        foreach (var item in viewModel.MaterialObligationViewModels)
        {
            item.MaterialName = Enum.Parse<MaterialType>(material);
            break;
        }
		_prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(externalIdsIcludingProducerExternalId, year)).ReturnsAsync(viewModel);

        // Act
        var response = await _controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        viewModel.GlassMaterialObligationViewModels.Count.Should().Be(0);
        viewModel.MaterialObligationViewModels.Count.Should().BeGreaterThan(0);
        _controller.ViewData["GlassOrNonGlassResource"].Should().Be(resource);
        _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        _prnServiceMock.Verify(x => x.GetRecyclingObligationsCalculation(It.IsAny<List<Guid>>(), year), Times.Once);
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
        _prnServiceMock.Setup(x => x.GetRecyclingObligationsCalculation(It.IsAny<List<Guid>>(), year)).ReturnsAsync(viewModel);

        // Act
        var response = await _controller.ObligationPerMaterial(material);

        var view = response.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().BeNull();
        _prnServiceMock.Verify(x => x.GetRecyclingObligationsCalculation(It.IsAny<List<Guid>>(), It.IsAny<int>()), Times.Never());
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

        // Act
        await _controller.FillViewModelFromSessionAsync(viewModel, 2024);

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
        var deadlineYear = currentYear + 1;
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
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear);

        // Assert
        viewModel.OrganisationRole.Should().BeEquivalentTo(OrganisationRoles.Producer);
        viewModel.OrganisationName.Should().BeEquivalentTo("Test Organisation");
        viewModel.NationId.Should().NotBeNull();
        viewModel.NationId.Value.Should().Be(nationId);
        viewModel.CurrentYear.Should().Be(currentYear);
        viewModel.DeadlineYear.Should().Be(deadlineYear);
    }

    [Theory]
    [TestCase(2)]
    [TestCase(4)]
    public async Task FillViewModelFromSessionAsync_Returns_Valid_ViewModelForComplianceSchemeMember(int nationId)
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
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear);

        // Assert
        viewModel.OrganisationRole.Should().BeEquivalentTo(OrganisationRoles.ComplianceScheme);
        viewModel.OrganisationName.Should().BeEquivalentTo("Test Organisation");
        viewModel.NationId.Should().NotBeNull();
        viewModel.NationId.Value.Should().Be(nationId);
        viewModel.CurrentYear.Should().Be(currentYear);
        viewModel.DeadlineYear.Should().Be(deadlineYear);
    }

    [Test]
    public async Task FillViewModelFromSessionAsync_Returns_ViewModelForComplianceSchemeMember_WhenSelectedCSIsNull()
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
        await _controller.FillViewModelFromSessionAsync(viewModel, currentYear);

        // Assert
        viewModel.OrganisationName.Should().BeNull();
        viewModel.NationId.Should().Be(0);
        viewModel.CurrentYear.Should().Be(currentYear);
        viewModel.DeadlineYear.Should().Be(deadlineYear);
    }

	[Test]
	public async Task GetChildOrganisationExternalIdsAsync_ShouldReturnEmptyResult_WhenOrganisationIsNull()
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

		// Act
		var result = await _controller.GetChildOrganisationExternalIdsAsync();

		// Assert
		result.Should().NotBeNull();
        result.Count.Should().Be(0);
	}

	[Test]
	public async Task GetChildOrganisationExternalIdsAsync_ShouldReturnExternalIds_WhenOrganisationRoleIsProducer()
	{
		// Arrange
        var organisationId = Guid.NewGuid();
        var childOrganisationId = Guid.NewGuid();
        var expectedExternalIds = new List<Guid> { organisationId, childOrganisationId };
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

        _prnServiceMock.Setup(p => p.GetChildOrganisationExternalIdsAsync(organisationId, null)).ReturnsAsync([childOrganisationId]);

		// Act
		var result = await _controller.GetChildOrganisationExternalIdsAsync();

		// Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(expectedExternalIds.Count);
        result.Should().BeEquivalentTo(expectedExternalIds);
	}

	[Test]
	public async Task GetChildOrganisationExternalIdsAsync_ShouldReturnExternalIds_WhenOrganisationRoleIsComplianceScheme()
	{
		// Arrange
		var organisationId = Guid.NewGuid();
		var complianceSchemeId = Guid.NewGuid();
        var expectedExternalIds = _fixture.CreateMany<Guid>().ToList();
		var session = new FrontendSchemeRegistrationSession
		{
			UserData = new UserData
			{
				Organisations = new List<Organisation>
				{
					new() {
                        Id = organisationId,
						OrganisationRole = OrganisationRoles.ComplianceScheme,
						Name = "Test Organisation",
						NationId = 1
					}
				}
			},
			RegistrationSession = new RegistrationSession
			{
				SelectedComplianceScheme = new ComplianceSchemeDto
				{
                    Id = complianceSchemeId,
                    Name = "Test Organisation",
					NationId = 1
				}
			}
		};
		_sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

		_prnServiceMock.Setup(p => p.GetChildOrganisationExternalIdsAsync(organisationId, complianceSchemeId)).ReturnsAsync(expectedExternalIds);

		// Act
		var result = await _controller.GetChildOrganisationExternalIdsAsync();

		// Assert
		result.Should().NotBeNull();
		result.Count.Should().Be(expectedExternalIds.Count);
		result.Should().BeEquivalentTo(expectedExternalIds);
	}
}
