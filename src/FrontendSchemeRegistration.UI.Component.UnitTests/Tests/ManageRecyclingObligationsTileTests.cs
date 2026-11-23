namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Application.Constants;
using Application.Enums;
using Extensions;
using FluentAssertions;
using Infrastructure;
using MockServer.WebApi;

public class ManageRecyclingObligationsTileTests
{
    private const string ComplianceSchemeHomePath = "/report-data/home-compliance-scheme";
    private const string WasteObligationsChooseYearPath = "/obligations/choose-year";

    private ComponentTestContext Context { get; } = new();

    [TestCase(true)]
    [TestCase(false)]
    public async Task WhenShowPrnsOnCdpEnabled_TileLinksToWasteObligationsChooseYear(bool showMultiYearObligations)
    {
        SetUp(showPrnsOnCdp: true, showMultiYearObligations);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(ComplianceSchemeHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().MatchRegex(
            $"href=\"https://understanding-obligations/cso/[0-9a-f-]{{36}}{WasteObligationsChooseYearPath}\"");
        content.Should().NotContain($"href=\"{PagePaths.Prns.ChooseYear}\"");
        content.Should().NotContain($"href=\"{PagePaths.Prns.ObligationsHome}\"");
    }

    [TestCase(true, PagePaths.Prns.ChooseYear)]
    [TestCase(false, PagePaths.Prns.ObligationsHome)]
    public async Task WhenShowPrnsOnCdpDisabled_TileKeepsPackagingLink(bool showMultiYearObligations, string expectedHref)
    {
        SetUp(showPrnsOnCdp: false, showMultiYearObligations);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(ComplianceSchemeHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"href=\"{expectedHref}\"");
        content.Should().NotContain(WasteObligationsChooseYearPath);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    private void SetUp(bool showPrnsOnCdp, bool showMultiYearObligations)
    {
        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:ShowPrn", "true" },
                { "FeatureManagement:CsocEnabled", "false" },
                { "FeatureManagement:ShowMultiYearObligations", showMultiYearObligations.ToString().ToLowerInvariant() },
                { "FeatureManagement:ShowPrnsOnCdp", showPrnsOnCdp.ToString().ToLowerInvariant() }
            },
            new WebApiOptions { ServiceRole = ServiceRoleConstants.Approved });
    }
}
