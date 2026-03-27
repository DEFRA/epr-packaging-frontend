namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using UI.Controllers;

[TestFixture]
public class AccountControllerTests
{
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
    }

    private static AccountController CreateController(bool isStubAuth)
    {
        var configData = new Dictionary<string, string>
        {
            { "IsStubAuth", isStubAuth.ToString() }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var controller = new AccountController(configuration);

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns<string>(url => !string.IsNullOrEmpty(url));
        mockUrlHelper
            .Setup(x => x.Content(It.IsAny<string>()))
            .Returns<string>(url => url);
        controller.Url = mockUrlHelper.Object;

        return controller;
    }

    [Test]
    public void SignIn_WhenCalled_ReturnsChallengeResult()
    {
        var controller = CreateController(isStubAuth: false);
        var scheme = _fixture.Create<string>();
        var redirectUri = _fixture.Create<string>();

        var result = controller.SignIn(scheme, redirectUri);

        result.Should().BeOfType<ChallengeResult>();
    }

    [Test]
    public void SignIn_WhenRedirectUriIsEmpty_SetsDefaultRedirectUri()
    {
        var controller = CreateController(isStubAuth: false);
        var scheme = _fixture.Create<string>();

        var result = controller.SignIn(scheme, string.Empty) as ChallengeResult;

        result.Properties.RedirectUri.Should().Be("~/");
    }

    [Test]
    public void SignOut_WhenStubAuth_ReturnsSignOutWithCookieSchemeOnly()
    {
        var controller = CreateController(isStubAuth: true);

        var result = controller.SignOut(null);

        var signOutResult = result.Should().BeOfType<SignOutResult>().Subject;
        signOutResult.AuthenticationSchemes.Should().ContainSingle()
            .Which.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
        signOutResult.Properties.RedirectUri.Should().Be("/services/account-details");
    }

    [Test]
    public void SessionSignOut_WhenStubAuth_ReturnsSignOutWithCookieSchemeOnly()
    {
        var controller = CreateController(isStubAuth: true);

        var result = controller.SessionSignOut(null);

        var signOutResult = result.Should().BeOfType<SignOutResult>().Subject;
        signOutResult.AuthenticationSchemes.Should().ContainSingle()
            .Which.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
        signOutResult.Properties.RedirectUri.Should().Be("/services/account-details");
    }
}
