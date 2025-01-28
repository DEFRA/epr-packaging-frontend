namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using System.Security.Claims;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UI.Extensions;

[TestFixture]
public class CorrelationIdProviderTests
{
    [Test]
    public void WhenHttpContextUserHasNoCorrelationId_ThenReturnNewGuid()
    {
        Mock<IHttpContextAccessor> httpContextAccessor = new();

        var provider = new CorrelationIdProvider(NullLogger<CorrelationIdProvider>.Instance, httpContextAccessor.Object);

        provider.GetCurrentCorrelationIdOrNew().Should().NotBe(Guid.Empty);
    }

    [Test]
    public void WhenHttpContextUserHasCorrelationId_ThenReturnIt()
    {
        Mock<IHttpContextAccessor> httpContextAccessor = new();

        var correlationId = Guid.NewGuid();

        httpContextAccessor
            .Setup(accessor => accessor.HttpContext.User)
            .Returns(new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(CorrelationClaimAction.CorrelationClaimType, correlationId.ToString())
                })));

        var provider = new CorrelationIdProvider(NullLogger<CorrelationIdProvider>.Instance, httpContextAccessor.Object);

        provider.GetCurrentCorrelationIdOrNew().Should().Be(correlationId);
    }
}