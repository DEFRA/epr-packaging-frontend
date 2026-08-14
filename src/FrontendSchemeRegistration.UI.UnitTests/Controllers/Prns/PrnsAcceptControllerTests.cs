namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using AutoFixture;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using Moq;

[TestFixture]
public class PrnsAcceptControllerTests
{
    private Mock<IPrnService> _mockPrnService;
    private Mock<IDownloadPrnService> _mockDownloadPrnService;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private PrnsAcceptController _sut;
    private static readonly IFixture _fixture = new Fixture();

    [SetUp]
    public void SetUp()
    {
        _mockPrnService = new Mock<IPrnService>();
        _mockDownloadPrnService = new Mock<IDownloadPrnService>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(false);
        _sut = new PrnsAcceptController(
            _mockPrnService.Object,
            _sessionManagerMock.Object,
            _mockDownloadPrnService.Object,
            _featureManagerMock.Object);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object
            },
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };
        var tempData = new TempDataDictionary(Mock.Of<HttpContext>(), Mock.Of<ITempDataProvider>());
        _sut.TempData = tempData;

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns("http://valid-url");
        _sut.Url = urlHelperMock.Object;
    }

    // Accept single Prn. Step 3 of 5
    [Test]
    public async Task AcceptSinglePrn_LoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(new PrnViewModel());

        // Act
        var result = await _sut.AcceptSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("AcceptSinglePrn");
        result.ViewData.Model.Should().NotBeNull();
    }

    [Theory]
    [TestCase("2024-12-01", "2025-01-01")]
    [TestCase("2024-12-15", "2025-01-15")]
    [TestCase("2024-12-01", "2025-01-31T23:59:59Z")]
    [TestCase("2024-12-01", "2025-02-01T00:00:00Z")]
    [TestCase("2024-12-01", "2025-02-01T00:00:01Z")]
    [TestCase("2025-01-01", "2025-02-01")]
    public async Task AcceptSinglePrn_Returns_CorrectView(string issuedDate, string acceptedOn)
    {
        var prn = _fixture.Create<PrnViewModel>();
        prn.IsDecemberWaste = true;
        prn.DateIssued = DateTime.Parse(issuedDate);
        prn.AvailableAcceptanceYears = [2025];
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(prn);

        // Act
        var result = await _sut.AcceptSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("AcceptSinglePrn");
    }

    [Test]
    public async Task AcceptSinglePrn_RedirectsToChooseAcceptanceYear_WhenDecemberWasteChoiceAvailable()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        var result = await _sut.AcceptSinglePrn(prnId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.ChooseAcceptanceYear));
        result.RouteValues!["id"].Should().Be(prnId);
    }

    [Test]
    public async Task AcceptSinglePrn_ReturnsView_WhenPrnIsNull()
    {
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync((PrnViewModel)null);

        // Act
        var result = await _sut.AcceptSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("AcceptSinglePrn");
        result.Model.Should().BeNull();
        _sessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Never);
    }

    [Test]
    public async Task AcceptSinglePrn_RedirectsToChooseAcceptanceYear_WhenSessionManagerReturnsNull()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        var result = await _sut.AcceptSinglePrn(prnId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.ChooseAcceptanceYear));
        result.RouteValues!["id"].Should().Be(prnId);
    }

    [Test]
    public async Task AcceptSinglePrn_ShowsConfirmPage_WhenYearAlreadyChosen()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession
                {
                    SelectedAcceptanceYearPrnId = prnId,
                    SelectedAcceptanceYear = 2027
                }
            });

        var result = await _sut.AcceptSinglePrn(prnId) as ViewResult;

        result.ViewName.Should().Be("AcceptSinglePrn");
        ((PrnViewModel)result.Model!).SelectedAcceptanceYear.Should().Be(2027);
    }

    [Test]
    public async Task ChooseAcceptanceYear_ReturnsView_WithPreFilledYear_WhenSessionMatchesPrnId()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession
                {
                    SelectedAcceptanceYearPrnId = prnId,
                    SelectedAcceptanceYear = 2027
                }
            });

        var result = await _sut.ChooseAcceptanceYear(prnId) as ViewResult;

        result.Model.Should().BeEquivalentTo(new ChooseAcceptanceYearViewModel
        {
            ExternalId = prnId,
            IsPrn = prn.IsPrn,
            AvailableAcceptanceYears = [2026, 2027],
            SelectedYear = 2027
        });
    }

    [Test]
    public async Task ChooseAcceptanceYear_ReturnsView_WhenChoiceAvailable()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        var result = await _sut.ChooseAcceptanceYear(prnId) as ViewResult;

        result.Model.Should().BeEquivalentTo(new ChooseAcceptanceYearViewModel
        {
            ExternalId = prnId,
            IsPrn = prn.IsPrn,
            AvailableAcceptanceYears = [2026, 2027],
            SelectedYear = null
        });
    }

    [Test]
    public async Task ChooseAcceptanceYear_ReturnsView_WhenSessionManagerReturnsNull()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        var result = await _sut.ChooseAcceptanceYear(prnId) as ViewResult;

        result.Model.Should().BeEquivalentTo(new ChooseAcceptanceYearViewModel
        {
            ExternalId = prnId,
            IsPrn = prn.IsPrn,
            AvailableAcceptanceYears = [2026, 2027],
            SelectedYear = null
        });
    }

    [Test]
    public async Task ChooseAcceptanceYear_RedirectsToAcceptSinglePrn_WhenFeatureDisabled()
    {
        var prnId = Guid.NewGuid();
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(false);

        var result = await _sut.ChooseAcceptanceYear(prnId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptSinglePrn));
        result.RouteValues!["id"].Should().Be(prnId);
    }

    [Test]
    public async Task ChooseAcceptanceYear_RedirectsToAcceptSinglePrn_WhenPrnIsNull()
    {
        var prnId = Guid.NewGuid();
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync((PrnViewModel)null);

        var result = await _sut.ChooseAcceptanceYear(prnId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptSinglePrn));
        result.RouteValues!["id"].Should().Be(prnId);
    }

    [Test]
    public async Task ChooseAcceptanceYear_Post_SavesSelectedYearAndRedirects()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        var result = await _sut.ChooseAcceptanceYear(prnId, new ChooseAcceptanceYearViewModel
        {
            SelectedYear = 2027
        }) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptSinglePrn));
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(
            It.IsAny<ISession>(),
            It.Is<FrontendSchemeRegistrationSession>(s =>
                s.PrnSession.SelectedAcceptanceYearPrnId == prnId
                && s.PrnSession.SelectedAcceptanceYear == 2027)), Times.Once);
    }

    [Test]
    public async Task ChooseAcceptanceYear_Post_SavesSelectedYearAndRedirects_WhenSessionManagerReturnsNull()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        var result = await _sut.ChooseAcceptanceYear(prnId, new ChooseAcceptanceYearViewModel
        {
            SelectedYear = 2027
        }) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptSinglePrn));
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(
            It.IsAny<ISession>(),
            It.Is<FrontendSchemeRegistrationSession>(s =>
                s.PrnSession.SelectedAcceptanceYearPrnId == prnId
                && s.PrnSession.SelectedAcceptanceYear == 2027)), Times.Once);
    }

    [Test]
    public async Task ChooseAcceptanceYear_Post_ReturnsView_WhenNoYearSelected()
    {
        var prnId = Guid.NewGuid();
        var prn = _fixture.Create<PrnViewModel>();
        prn.ExternalId = prnId;
        prn.ApprovalStatus = PrnStatus.AwaitingAcceptance;
        prn.AvailableAcceptanceYears = [2026, 2027];
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync(prn);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);

        var result = await _sut.ChooseAcceptanceYear(prnId, new ChooseAcceptanceYearViewModel()) as ViewResult;

        result.Should().NotBeNull();
        _sut.ModelState.IsValid.Should().BeFalse();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Never);
    }

    [Test]
    public async Task ChooseAcceptanceYear_Post_RedirectsToAcceptSinglePrn_WhenFeatureDisabled()
    {
        var prnId = Guid.NewGuid();
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(false);

        var result = await _sut.ChooseAcceptanceYear(prnId, new ChooseAcceptanceYearViewModel()) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptSinglePrn));
        result.RouteValues!["id"].Should().Be(prnId);
        _mockPrnService.Verify(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task ChooseAcceptanceYear_Post_RedirectsToAcceptSinglePrn_WhenPrnIsNull()
    {
        var prnId = Guid.NewGuid();
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(prnId)).ReturnsAsync((PrnViewModel)null);

        var result = await _sut.ChooseAcceptanceYear(prnId, new ChooseAcceptanceYearViewModel { SelectedYear = 2027 }) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptSinglePrn));
        result.RouteValues!["id"].Should().Be(prnId);
    }

    // Step 4, return after login timeout
    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnGetRedirectToSelectPrns()
    {
        // Act
        var result = await _sut.ConfirmAcceptSinglePrnPassThrough() as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.SelectMultiplePrns));
        result.ControllerName.Should().Be("Prns");
    }

    // Accept single Prn. Step 4 of 5
    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPostRedirectToAcceptedPage()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        result.ControllerName.Should().Be("PrnsAccept");
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId, null), Times.Once);
    }

    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPost_WhenSessionHasDifferentPrnId_DoesNotClearOrPassObligationYear()
    {
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession
                {
                    SelectedAcceptanceYearPrnId = Guid.NewGuid(),
                    SelectedAcceptanceYear = 2027
                }
            });

        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId, null), Times.Once);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPost_WhenSessionHasNoAcceptanceYearSelection_DoesNotClearOrPassObligationYear()
    {
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession());

        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId, null), Times.Once);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPost_PassesChosenObligationYear()
    {
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession
                {
                    SelectedAcceptanceYearPrnId = model.ExternalId,
                    SelectedAcceptanceYear = 2027
                }
            });

        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId, "2027"), Times.Once);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(
            It.IsAny<ISession>(),
            It.Is<FrontendSchemeRegistrationSession>(s =>
                s.PrnSession.SelectedAcceptanceYearPrnId == null
                && s.PrnSession.SelectedAcceptanceYear == null)), Times.Once);
    }

    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPost_DoesNotClearSession_WhenSessionPrnIdDiffers()
    {
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession
                {
                    SelectedAcceptanceYearPrnId = Guid.NewGuid(),
                    SelectedAcceptanceYear = 2027
                }
            });

        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId, null), Times.Once);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Never);
    }

    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPost_DoesNotClearSession_WhenSessionHasNoSelectedAcceptanceYearPrnId()
    {
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession()
            });

        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId, null), Times.Once);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Never);
    }

    // Accept single Prn. Step 5 of 5
    [Test]
    public async Task AcceptedPrn_ReturnsCorrectView_WhenPernOrPrnIsAccepted()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "ACCEPTED"
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.AcceptedPrn(model.ExternalId) as ViewResult;

        result.ViewName.Should().BeNull();
    }

    // Accept single Prn. Step 5 of 5 incorrect status
    [Test]
    public async Task AcceptedPrn_RedirectToLandingPage_WhenPernOrPrnIsNotAccepted()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "AWAITING ACCEPTANCE"
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.AcceptedPrn(model.ExternalId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsObligationController.ObligationsHome));
        result.ControllerName.Should().Be("PrnsObligation");
    }

    // Accept single Prn. Step 5 of 5 null status
    [Test]
    public async Task AcceptedPrn_RedirectToLandingPage_WhenPernOrPrnDoesNotExist()
    {
        // Act
        var result = await _sut.AcceptedPrn(Guid.NewGuid()) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsObligationController.ObligationsHome));
        result.ControllerName.Should().Be("PrnsObligation");
    }

    // Accept multiple Prns. Step 2 of 5 recover from timeout
    [Test]
    public async Task AcceptMultiplePrnsPassThrough_OnGet_RedirectToSelectMultiplePrns()
    {
        var result = await _sut.AcceptMultiplePrnsPassThrough();

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("SelectMultiplePrns");
    }

    // Accept multiple Prns. Step 2 of 5 zero selections error
    [Test]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToSelectMultiplePrns_IfNoneIsSelectedForAcceptance()
    {
        var model = _fixture.Create<PrnListViewModel>();
        model.Prns.ForEach(x => x.IsSelected = false);
        model.PreviousSelectedPrns.ForEach(x => x.IsSelected = false);

        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("SelectMultiplePrns");
    }

    // Accept multiple Prns. Step 2 of 5 zero model is null error
    [Test]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToSelectMultiplePrns_WhenModelIsNulll()
    {
        PrnListViewModel model = new();
        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("SelectMultiplePrns");
    }

    // Accept multiple Prns. Step 2 of 5
    [Test]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToAcceptMultiplePrns_IfPrnsAreSelectedForAcceptance()
    {
        var model = _fixture.Create<PrnListViewModel>();
        model.Prns[0].IsSelected = true;

        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("AcceptMultiplePrns");
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
    }

    [Test]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToAcceptMultiplePrnsByCallingGetAllPrnsAndSavingThemOnSession_IfAllPrnsIsSelected()
    {
        var model = _fixture.Create<PrnListViewModel>();
        var allPrns = _fixture.Create<PrnListViewModel>();

        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(allPrns);

        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("AcceptMultiplePrns");
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
    }

    // Accept multiple Prns. Step 3 of 5 pass PRN id
    [Test]
    public async Task AcceptMultiplePrns_RemoveIdFromSessionIfIdIsNotNull()
    {
        var model = _fixture.Create<PrnListViewModel>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PrnSession = new PrnSession
            {
                SelectedPrnIds = model.Prns.Select(x => x.ExternalId).ToList()
            }
        });
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);
        var removedPrnNumber = model.Prns[0].PrnOrPernNumber;
        // Act
        var result = await _sut.AcceptMultiplePrns(model.Prns[0].ExternalId) as ViewResult;
        ((PrnListViewModel)result.Model).RemovedPrn.PrnNumber.Should().Be(removedPrnNumber);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
    }


    [Test]
    public async Task AcceptMultiplePrns_ReturnsView_WhenSessionManagerReturnsNull()
    {
        var model = _fixture.Create<PrnListViewModel>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        // Act
        var result = await _sut.AcceptMultiplePrns(model.Prns[0].ExternalId) as ViewResult;

        // Assert - falls back to a fresh session, so no PRN is pre-selected and nothing is removed
        result.Should().NotBeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
    }

    [Test]
    public async Task AcceptMultiplePrns_DoesNotRemoveFromSession_WhenIdIsEmpty()
    {
        var model = _fixture.Create<PrnListViewModel>();
        model.RemovedPrn = null;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PrnSession = new PrnSession
            {
                SelectedPrnIds = model.Prns.Select(x => x.ExternalId).ToList()
            }
        });
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptMultiplePrns(Guid.Empty) as ViewResult;

        ((PrnListViewModel)result.Model).RemovedPrn.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Never);
    }

    // Step 4, return after login timeout
    [Test]
    public async Task ConfirmAcceptMultiplePrnsPassThrough_OnGetRedirectToSelectPrns()
    {
        // Act
        var result = await _sut.ConfirmAcceptMultiplePrnsPassThrough() as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.SelectMultiplePrns));
        result.ControllerName.Should().Be("Prns");
    }

    // Accept multiple Prns. Step 4 of 5
    [Test]
    public async Task ConfirmAcceptMultiplePrnsPassThrough_OnPostRedirectToAcceptedPrnsBySettingTempData()
    {
        var model = _fixture.Create<PrnListViewModel>();
        _mockPrnService.Setup(x => x.AcceptPrnsAsync(It.IsAny<Guid[]>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ConfirmAcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("AcceptedPrns");
        _mockPrnService.VerifyAll();
    }

    // Accept multiple PRNs. Step 5 of 5
    [Test]
    public async Task AcceptedPrns_ConstructCorrectVMFromSession()
    {
        var acceptedPrns = _fixture.CreateMany<Guid>().ToList();
        var model = _fixture.Create<PrnListViewModel>();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new FrontendSchemeRegistrationSession
        {
            PrnSession = new PrnSession
            {
                SelectedPrnIds = acceptedPrns,
                InitialNoteTypes = string.Empty
            }
        });

        model.Prns[0].ExternalId = acceptedPrns[0];
        model.Prns[1].ExternalId = acceptedPrns[1];
        model.Prns[2].ExternalId = acceptedPrns[2];
        model.Prns[0].NoteType = model.Prns[1].NoteType = model.Prns[2].NoteType = "PRN";
        model.Prns[0].ObligationYear = model.Prns[1].ObligationYear = model.Prns[2].ObligationYear = 2025;

        _mockPrnService.Setup(x => x.GetAllAcceptedPrnsAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptedPrns() as ViewResult;

        result.Model.Should().BeEquivalentTo(new AcceptedPrnsModel()
        {
            Count = 3,
            NoteTypes = "PRNs",
            ObligationYears = "2025",
            Details = new List<AcceptedDetails>
            {
                new(model.Prns[0].Material, model.Prns[0].Tonnage),
                new(model.Prns[1].Material, model.Prns[1].Tonnage),
                new(model.Prns[2].Material, model.Prns[2].Tonnage)
            }
        });

        _mockPrnService.Verify(x => x.GetAllAcceptedPrnsAsync(), Times.Once);
    }

    [Test]
    public async Task AcceptedPrns_ReturnsEmptySummary_WhenSessionManagerReturnsNull()
    {
        var model = _fixture.Create<PrnListViewModel>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);
        _mockPrnService.Setup(x => x.GetAllAcceptedPrnsAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptedPrns() as ViewResult;

        // Assert - a fresh session has no SelectedPrnIds, so none of the fetched PRNs are treated as "just updated"
        var summary = result.Model.As<AcceptedPrnsModel>();
        summary.Count.Should().Be(0);
        summary.Details.Should().BeEmpty();
        summary.ObligationYears.Should().BeEmpty();
    }

    [Test]
    public async Task DownloadPrn_CallsDownloadPrnAsync_AndReturnsOkObjectResult()
    {
        // Arrange
        var prnId = Guid.NewGuid();
        var expectedResult = new OkObjectResult(new { fileName = "PRN123", htmlContent = "<html><body>Sample Content</body></html>" });

        _mockDownloadPrnService
            .Setup(x => x.DownloadPrnAsync(prnId, "AcceptedPrn", It.IsAny<ActionContext>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.DownloadPrn(prnId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(new { fileName = "PRN123", htmlContent = "<html><body>Sample Content</body></html>" });

        _mockDownloadPrnService.Verify(x => x.DownloadPrnAsync(prnId, "AcceptedPrn", It.IsAny<ActionContext>()), Times.Once);
    }
}
