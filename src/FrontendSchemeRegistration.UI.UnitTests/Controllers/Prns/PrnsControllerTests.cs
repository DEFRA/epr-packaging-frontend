namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using AutoFixture.NUnit3;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using Moq;

public class PrnControllerTests
{
    private Mock<IPrnService> _mockPrnService;

    private PrnsController _sut;

    [SetUp]
    public void SetUp()
    {
        this._mockPrnService = new Mock<IPrnService>();
        var fakeTimeProvider = new FakeTimeProvider();
        _sut = new PrnsController(_mockPrnService.Object, fakeTimeProvider);
    }

    [Theory]
    [AutoData]
    public async Task ViewAllPrns_Returns_AllPrns(PrnListViewModel model)
    {
        _mockPrnService.Setup(x => x.GetAllPrnsAsync()).ReturnsAsync(model);
        var result = await _sut.ViewAllPrns();
        _mockPrnService.Verify(x => x.GetAllPrnsAsync(), Times.Once);
        var view = result.Should().BeOfType<ViewResult>().Which;
        view.Model.Should().BeEquivalentTo(model);
    }

    [Theory]
    [AutoData]
    public async Task HomePagePrn_CallsGetPrnsAwaitingAcceptanceAsync(PrnListViewModel model)
    {
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        var result = await _sut.HomePagePrn();

        var view = result.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().Be("HomePagePrn");
        _mockPrnService.VerifyAll();
    }

    // Accept or reject single or multiple Prns. Step 1 of 5 zero selections
    [Theory]
    [AutoData]
    public async Task SelectMultiplePrns_AddErrorsIntoModelStateIfAnyErrors(PrnListViewModel model, string error)
    {
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        // Act
        var result = await _sut.SelectMultiplePrns(error);

        // Assert
        _mockPrnService.Verify(x => x.GetPrnsAwaitingAcceptanceAsync(), Times.Once);
        var view = result.Should().BeOfType<ViewResult>().Which;
        view.Model.Should().BeEquivalentTo(model);
        view.ViewData.ModelState.Count.Should().Be(1);
        view.ViewData.ModelState.GetModelStateEntry("Error").Value.Errors.Select(x => x.ErrorMessage)
           .Should().Contain("select_one_or_more_prns_or_perns_to_accept_them");
    }

    // Accept or reject single Prn. Step 2 of 5 when accepting or rejecting single PRN
    [Test]
    public async Task SelectSinglePrn_LoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(new PrnViewModel());

        // Act
        var result = await _sut.SelectSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().BeNull();
        result.ViewData.Model.Should().NotBeNull();

        _sut.ViewData.Should().ContainKey("BackLinkToDisplay");
        _sut.ViewData.Should().ContainKey("DecemberWasteRulesApply");
    }
}