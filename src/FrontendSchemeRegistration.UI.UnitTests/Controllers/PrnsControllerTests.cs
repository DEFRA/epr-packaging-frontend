namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using AutoFixture;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

[TestFixture]
public class PrnsControllerTests
{
    private PrnsController _prnsController;
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        var mockPrnService = new Mock<IPrnService>();
        mockPrnService.Setup(x => x.GetPrnById(It.IsAny<int>())).Returns(new UI.ViewModels.Prns.PrnViewModel());
        _prnsController = new PrnsController(mockPrnService.Object);
    }

    [Test]
    public async Task
        GivenOnLoadAcceptPrnPage_WhenCalledByDefault_ThenLoadTheStandardResponse()
    {
        // Act
        var result = await _prnsController.ConfirmAcceptSinglePrn(1) as ViewResult;

        // Assert
        result.ViewName.Should().BeNull();
        result.ViewData.Model.Should().NotBeNull();
    }
}