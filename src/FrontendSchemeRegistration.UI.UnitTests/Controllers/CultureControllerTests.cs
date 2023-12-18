namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Text;
using Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using UI.Controllers.Culture;

public class CultureControllerTests
{
    private const string ReturnUrl = "returnUrl";
    private const string CultureEn = "en";

    private readonly Mock<IResponseCookies> _responseCookiesMock = new();
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly CultureController _systemUnderTest = new();

    [SetUp]
    public void Setup()
    {
        _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
        _httpContextMock.Setup(x => x.Session).Returns(_sessionMock.Object);
    }

    [Test]
    public void CultureController_UpdateCulture_RedirectsToReturnUrlWithCulture()
    {
        // Arrange
        _httpContextMock!
            .Setup(x => x.Response.Cookies)
            .Returns(_responseCookiesMock!.Object);

        var cultureBytes = Encoding.UTF8.GetBytes(CultureEn);

        // Act
        var result = _systemUnderTest!.UpdateCulture(CultureEn, ReturnUrl);

        // Assert
        result.Url.Should().Be(ReturnUrl);
        _sessionMock.Verify(x => x.Set(Language.SessionLanguageKey, cultureBytes), Times.Once);
    }
}