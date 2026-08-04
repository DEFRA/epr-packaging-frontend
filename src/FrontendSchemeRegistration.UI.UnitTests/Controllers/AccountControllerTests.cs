namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Options;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using UI.Controllers;

[TestFixture]
public class AccountControllerTests
{
    private AccountController _accountController;
    private Fixture _fixture;
    private Mock<ISession> _sessionMock;

    [SetUp]
    public void SetUp()
    {
        _sessionMock = new Mock<ISession>();
        _sessionMock.SetupGet(x => x.IsAvailable).Returns(true);

        _accountController = new AccountController(
            Options.Create(new CsocOptions
            {
                WasteObligationsBaseAddress = "http://localhost:3000"
            }),
            new Mock<IFeatureManager>().Object);
        _fixture = new Fixture();
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns<string>(url => !string.IsNullOrEmpty(url));
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(context => $"/{context.Action}/{context.Controller}");
        _accountController.Url = mockUrlHelper.Object;
        _accountController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = _sessionMock.Object
            }
        };
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

    [Test]
    public void ClearSession_Should_Clear_Session_And_Return_SignOutResult()
    {
        var result = _accountController.ClearSession();

        _sessionMock.Verify(x => x.Clear(), Times.Once);
        result.Should().BeOfType<SignOutResult>();
    }

    [Test]
    public async Task SignOut_Should_Clear_Session_Before_SignOut()
    {
        var result = await _accountController.SignOut(null);

        _sessionMock.Verify(x => x.Clear(), Times.Once);
        result.Should().BeOfType<SignOutResult>();
    }

    [Test]
    public void SessionSignOut_Should_Clear_Session_Before_SignOut()
    {
        var result = _accountController.SessionSignOut(null);

        _sessionMock.Verify(x => x.Clear(), Times.Once);
        result.Should().BeOfType<SignOutResult>();
    }

    [Test]
    public void KeepSessionAlive_Should_Set_LastPing()
    {
        var result = _accountController.KeepSessionAlive();

        _sessionMock.Verify(
            x => x.Set("LastPing", It.IsAny<byte[]>()),
            Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }
}
