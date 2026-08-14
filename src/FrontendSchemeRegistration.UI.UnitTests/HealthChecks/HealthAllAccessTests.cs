namespace FrontendSchemeRegistration.UI.UnitTests.HealthChecks;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using UI.HealthChecks;

[TestFixture]
public class HealthAllAccessTests
{
    [Test]
    public void IsValid_WhenHeaderMatchesConfiguredToken_ReturnsTrue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Health-Check-Token"] = "expected-token";

        var result = HealthAllAccess.IsValid(context.Request, new HealthAllOptions { Token = "expected-token" });

        result.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("incorrect-token")]
    public void IsValid_WhenHeaderIsMissingOrInvalid_ReturnsFalse(string? token)
    {
        var context = new DefaultHttpContext();
        if (token is not null)
        {
            context.Request.Headers["X-Health-Check-Token"] = token;
        }

        var result = HealthAllAccess.IsValid(context.Request, new HealthAllOptions { Token = "expected-token" });

        result.Should().BeFalse();
    }
}
