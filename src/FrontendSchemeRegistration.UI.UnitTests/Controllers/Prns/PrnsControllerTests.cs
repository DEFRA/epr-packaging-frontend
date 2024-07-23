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

    [Test]
    public void Get_ReturnsCorrectView_IfPernOrPrnExists()
    {
        var model = new PrnViewModel();
        model.ApprovalStatus = "ACCEPTED";
        model.PrnOrPernNumber = "test";

        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(model);
        // Act
        var result = _sut.AcceptedPrn(model.Id) as ViewResult;

        result.ViewName.Should().BeNull();
    }

    [Test]
    public void Get_RedirectToLandingPage_IfPernOrPrnDoesnotExists()
    {
        var model = new PrnViewModel();
        // Act
        var result = _sut.AcceptedPrn(0) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
        result.ControllerName.Should().Be("Prns");
    }

    [Test]
    public async Task
    GivenOnLoadAcceptPrnPage_WhenCalledByDefault_ThenLoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(new UI.ViewModels.Prns.PrnViewModel());
        // Act
        var prn = new UI.ViewModels.Prns.PrnViewModel();
        var result = await _sut.ConfirmAcceptSinglePrn(prn) as ViewResult;

        // Assert
        result.ViewName.Should().BeNull();
        result.ViewData.Model.Should().NotBeNull();
    }
}