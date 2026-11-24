namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Application.Constants;
using Application.Enums;
using Extensions;
using FluentAssertions;
using Infrastructure;
using MockServer.WebApi;

public class SearchPrnsLinksTests
{
    private const string SearchPath = "/report-data/view-awaiting-acceptance";
    private const string AwaitingAcceptanceNoteId = "00000000-0000-0000-0000-000000000001";
    private const string AcceptedNoteId = "00000000-0000-0000-0000-000000000005";

    private ComponentTestContext Context { get; } = new();

    [Test]
    public async Task WhenShowPrnsOnCdpEnabled_NoteNumbersLinkToTheEprNextGenPrnPageForTheYear()
    {
        SetUp(showPrnsOnCdp: true);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(SearchPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        foreach (var noteId in new[] { AwaitingAcceptanceNoteId, AcceptedNoteId })
        {
            content.Should().MatchRegex(
                $"href=\"https://understanding-obligations/(producer|cso)/[0-9a-f-]{{36}}/prns/{noteId}\\?year=[0-9]{{4}}\"");
            content.Should().NotContain($"href=\"/report-data/{PagePaths.Prns.ShowSelected}/{noteId}\"");
        }
    }

    [Test]
    public async Task WhenShowPrnsOnCdpDisabled_NoteNumbersKeepThePackagingPrnPage()
    {
        SetUp(showPrnsOnCdp: false);
        await Context.Client.AuthenticateDefaultUser();

        var response = await Context.Client.GetAsync(SearchPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"href=\"/report-data/{PagePaths.Prns.ShowSelected}/{AwaitingAcceptanceNoteId}\"");
        content.Should().Contain($"href=\"/report-data/{PagePaths.Prns.ShowSelected}/{AcceptedNoteId}\"");
        content.Should().NotContain("understanding-obligations/producer/");
        content.Should().NotContain("understanding-obligations/cso/");
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    private void SetUp(bool showPrnsOnCdp)
    {
        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:ShowPrn", "true" },
                { "FeatureManagement:ShowPrnsOnCdp", showPrnsOnCdp.ToString().ToLowerInvariant() }
            },
            new WebApiOptions { ServiceRole = ServiceRoleConstants.Approved });
    }
}
