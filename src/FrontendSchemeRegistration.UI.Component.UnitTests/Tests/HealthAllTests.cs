using System.Net;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

public class HealthAllTests
{
    private ComponentTestContext Context { get; } = new();

    [SetUp]
    public void SetUp()
    {
        Context.SetUp(additionalConfig: new Dictionary<string, string?>
        {
            ["Health:All:Token"] = "health-test-token",
        });
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    [Test]
    public async Task WhenShallowHealthIsRequestedWithoutTheDeepQuery_ShouldReturnTheShallowHealthReport()
    {
        var response = await Context.Client.GetAsync(
            "/admin/health/all",
            new Dictionary<string, string> { ["X-Health-Check-Token"] = "health-test-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .ScrubMember("durationMs");
    }

    [Test]
    public async Task WhenDeepHealthIsRequested_ShouldReturnTheGatewayHealthReport()
    {
        var response = await Context.Client.GetAsync(
            "/admin/health/all?deep=true",
            new Dictionary<string, string> { ["X-Health-Check-Token"] = "health-test-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .ScrubMember("durationMs");
    }

    [Test]
    public async Task WhenTheHealthHopHeaderIsInvalid_ShouldReturnBadRequest()
    {
        var response = await Context.Client.GetAsync(
            "/admin/health/all?deep=true",
            new Dictionary<string, string>
            {
                ["X-Health-Check-Token"] = "health-test-token",
                ["X-EPR-Health-Check-Hop"] = "invalid",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
