namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Options;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using UI.Controllers;

[TestFixture]
public class AccountControllerTests
{
    private AccountController _accountController;
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _accountController = new AccountController(
            Options.Create(new CsocOptions
            {
                WasteObligationsBaseAddress = "http://localhost:3000"
            }));
        _fixture = new Fixture();
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns<string>(url => !string.IsNullOrEmpty(url));
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(context => $"/{context.Action}/{context.Controller}");
        _accountController.Url = mockUrlHelper.Object;
    }

    [Test]
    public void SignIn_WhenCalled_ReturnsChallengeResult()
    {
        // Arrange
        var scheme = _fixture.Create<string>();
        var redirectUri = _fixture.Create<string>();

        // Act
        var result = _accountController.SignIn(scheme, redirectUri);

        // Assert
        result.Should().BeOfType<ChallengeResult>();
    }

    [Test]
    public void SignIn_WhenRedirectUriIsEmpty_SetsDefaultRedirectUri()
    {
        // Arrange
        var scheme = _fixture.Create<string>();
        var redirectUri = string.Empty;

        // Act
        var result = _accountController.SignIn(scheme, redirectUri) as ChallengeResult;

        // Assert
        result.Properties.RedirectUri.Should().BeNull();
    }
}
