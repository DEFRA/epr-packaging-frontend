namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Moq;

[TestFixture]
public class PrnsControllerTests
{
    private Mock<IPrnService> _mockPrnService;

    private PrnsController _sut;

    [SetUp]
    public void SetUp()
    {
        this._mockPrnService = new Mock<IPrnService>();
        _sut = new PrnsController(_mockPrnService.Object);
    }

    // Step 2
    [Test]
    public async Task AcceptSinglePrn_LoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(new PrnViewModel());

        // Act
        var result = await _sut.AcceptSinglePrn(0) as ViewResult;

        // Assert
        result.ViewName.Should().BeNull();
        result.ViewData.Model.Should().NotBeNull();
    }

    // Step 3
    [Test]
    public async Task ConfirmAcceptSinglePrn_LoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(new PrnViewModel());

        // Act
        var result = await _sut.ConfirmAcceptSinglePrn(new PrnViewModel()) as ViewResult;

        // Assert
        result.ViewName.Should().BeNull();
        result.ViewData.Model.Should().NotBeNull();
    }

    // Step 4
    [Test]
    public async Task ConfirmAcceptPrn_RedirectToAcceptedPage()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "AWAITING ACCEPTANCE"
        };
        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(model);

        // Act
        var result = await _sut.ConfirmAcceptPrn(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(PrnsController.AcceptedPrn));
        result.ControllerName.Should().Be("Prns");
        _mockPrnService.Verify(x => x.UpdatePrnStatus(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    // Step 5
    [Test]
    public void AcceptedPrn_ReturnsCorrectView_WhenPernOrPrnIsAccepted()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "ACCEPTED"
        };
        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(model);

        // Act
        var result = _sut.AcceptedPrn(model.Id) as ViewResult;

        result.ViewName.Should().BeNull();
    }

    // Step 5
    [Test]
    public void AcceptedPrn_RedirectToLandingPage_WhenPernOrPrnIsNotAccepted()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "AWAITING ACCEPTANCE"
        };
        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(model);

        // Act
        var result = _sut.AcceptedPrn(model.Id) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
        result.ControllerName.Should().Be("Prns");
    }

    // Step 5
    [Test]
    public void AcceptedPrn_RedirectToLandingPage_WhenPernOrPrnDoesNotExist()
    {
        // Act
        var result = _sut.AcceptedPrn(0) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
        result.ControllerName.Should().Be("Prns");
    }
}