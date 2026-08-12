namespace FrontendSchemeRegistration.UI.UnitTests.HealthChecks;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using UI.HealthChecks;

[TestFixture]
public class AggregateHealthHopTests
{
    [Test]
    public void TryRead_WhenHeaderIsMissing_StartsAtZero()
    {
        var request = new DefaultHttpContext().Request;

        var isValid = AggregateHealthHop.TryRead(request, 2, out var hop);

        isValid.Should().BeTrue();
        hop.Should().Be(0);
    }

    [TestCase("-1")]
    [TestCase("3")]
    [TestCase("invalid")]
    [TestCase("999999999999999999999")]
    public void TryRead_WhenHeaderIsInvalid_ReturnsFalse(string headerValue)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[AggregateHealthHop.HeaderName] = headerValue;

        var isValid = AggregateHealthHop.TryRead(request, 2, out _);

        isValid.Should().BeFalse();
    }

    [Test]
    public void TryRead_WhenHeaderIsRepeated_ReturnsFalse()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[AggregateHealthHop.HeaderName] = new StringValues(["1", "2"]);

        var isValid = AggregateHealthHop.TryRead(request, 2, out _);

        isValid.Should().BeFalse();
    }
}
