namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using AutoFixture;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services;
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
        _prnsController = new PrnsController(null);
        _fixture = new Fixture();
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns<string>(url => !string.IsNullOrEmpty(url));
        _prnsController.Url = mockUrlHelper.Object;
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