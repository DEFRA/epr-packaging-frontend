namespace FrontendSchemeRegistration.UI.UnitTests.Sessions;

using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using UI.Constants;
using UI.Sessions;

[TestFixture]
public class SessionRequestCultureProviderTests
{
    private Mock<ISession> _sessionMock;
    private SessionRequestCultureProvider _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _sessionMock = new Mock<ISession>();
        _systemUnderTest = new SessionRequestCultureProvider();
    }

    [Test]
    public async Task DetermineProviderCultureResult_WhenSessionHasNoLanguageKey_ReturnsEnglish()
    {
        var httpContext = new DefaultHttpContext { Session = _sessionMock.Object };

        var result = await _systemUnderTest.DetermineProviderCultureResult(httpContext);

        result.Cultures[0].Value.Should().Be(Language.English);
        result.UICultures[0].Value.Should().Be(Language.English);
    }

    [Test]
    public async Task DetermineProviderCultureResult_WhenSessionHasLanguageKey_ReturnsSessionCulture()
    {
        var storedBytes = Encoding.UTF8.GetBytes("cy");
        _sessionMock
            .Setup(x => x.TryGetValue(Language.SessionLanguageKey, out storedBytes))
            .Returns(true);
        var httpContext = new DefaultHttpContext { Session = _sessionMock.Object };

        var result = await _systemUnderTest.DetermineProviderCultureResult(httpContext);

        result.Cultures[0].Value.Should().Be("cy");
        result.UICultures[0].Value.Should().Be("cy");
    }
}
