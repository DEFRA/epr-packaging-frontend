namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using AutoFixture;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;

public class PrnsRejectControllerTests
{
    private Mock<IPrnService> _mockPrnService;
    private Mock<IDownloadPrnService> _mockDownloadPrnService;

    private PrnsRejectController _sut;

    private static readonly IFixture _fixture = new Fixture();

    [SetUp]
    public void SetUp()
    {
        this._mockPrnService = new Mock<IPrnService>();
        _mockDownloadPrnService = new Mock<IDownloadPrnService>();
        _sut = new PrnsRejectController(_mockPrnService.Object, _mockDownloadPrnService.Object);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object
            },
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };
    }

    // Reject single Prn. Step 3 of 5
    [Test]
    public async Task RejectSinglePrn_LoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(new PrnViewModel());

        // Act
        var result = await _sut.RejectSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("RejectSinglePrn");
        result.ViewData.Model.Should().NotBeNull();
    }

    // Step 4, return after login timeout
    [Test]
    public async Task ConfirmRejectSinglePrnPassThrough_OnGetRedirectToSelectPrns()
    {
        // Act
        var result = await _sut.ConfirmRejectSinglePrnPassThrough() as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.SelectMultiplePrns));
        result.ControllerName.Should().Be("Prns");
    }

    // Reject single Prn. Step 4 of 5
    [Test]
    public async Task ConfirmRejectSinglePrnPassThrough_OnPostRedirectToRejectedPage()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.ConfirmRejectSinglePrnPassThrough(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(PrnsRejectController.RejectedPrn));
        result.ControllerName.Should().Be("PrnsReject");
        _mockPrnService.Verify(x => x.RejectPrnAsync(model.ExternalId), Times.Once);
    }

    // Reject single PRN. Step 5 of 5
    [Test]
    public async Task RejectedPrn_ReturnsCorrectView_WhenPernOrPrnIsRejected()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "REJECTED"
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.RejectedPrn(model.ExternalId) as ViewResult;

        result.ViewName.Should().BeNull();
    }

    // Reject single PRN. Step 5 of 5 incorrect approval status
    [Test]
    public async Task RejectedPrn_RedirectToLandingPage_WhenPernOrPrnIsNotRejected()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "AWAITING ACCEPTANCE"
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.RejectedPrn(model.ExternalId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsObligationController.ObligationsHome));
        result.ControllerName.Should().Be("PrnsObligation");
    }

    // Reject single PRN. Step 5 of 5 Prn is null
    [Test]
    public async Task RejectedPrn_RedirectToLandingPage_WhenPernOrPrnIsMissing()
    {
        // Act
        var result = await _sut.RejectedPrn(Guid.NewGuid()) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsObligationController.ObligationsHome));
        result.ControllerName.Should().Be("PrnsObligation");
    }

    [Test]
    public async Task DownloadPrn_CallsDownloadPrnAsync_AndReturnsOkObjectResult()
    {
        // Arrange
        var prnId = Guid.NewGuid();
        var expectedResult = new OkObjectResult(new { fileName = "PRN123", htmlContent = "<html><body>Sample Content</body></html>" });

        _mockDownloadPrnService
            .Setup(x => x.DownloadPrnAsync(prnId, "RejectedPrn", It.IsAny<ActionContext>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.DownloadPrn(prnId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(new { fileName = "PRN123", htmlContent = "<html><body>Sample Content</body></html>" });

        _mockDownloadPrnService.Verify(x => x.DownloadPrnAsync(prnId, "RejectedPrn", It.IsAny<ActionContext>()), Times.Once);
    }
}