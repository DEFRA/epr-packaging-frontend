namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.Enums;
using Application.Extensions;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using FluentAssertions;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using MockServer.WebApi;
using Sessions;

public class ManageObligationsPageTests
{
    private const string ObligationsHomePath = "/report-data/manage-your-recycling-obligations";

    // Must match ConfigBuilder StartupUtcTimestampOverride used by the ComponentTest host.
    private static readonly int ComplianceYear =
        DateTimeOffset.Parse("2026-03-27T08:58:00Z").GetComplianceYear();

    private ComponentTestContext Context { get; } = new();

    [Test]
    public async Task WhenNoObligations_AndMultiYearEnabled_ShowsAlternativeContent()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenNoObligations_AndMultiYearDisabled_ShowsLegacyContent()
    {
        SetUp(
            showMultiYearObligations: false,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenObligationsPresent_AndMultiYearEnabled_ShowsExistingAdvisoryText()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenMultiYearEnabled_WhatToDoNext_ShowsComplianceYearInHeadingAndLinks()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenMultiYearDisabled_WhatToDoNext_OmitsComplianceYearFromHeadingAndLinks()
    {
        SetUp(
            showMultiYearObligations: false,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenFutureYearSelected_InDecemberJanuaryFlashWindow_WithDecemberWastePrnAwaitingAcceptance_ShowsDetailsSummaryAccordion()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet,
            prnOrganisationData: WebApiOptions.PrnOrganisationDataType.DecemberWasteAwaitingAcceptance,
            startupUtcTimestampOverride: "2026-12-15T08:00:00Z");
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear + 1);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenFutureYearSelected_InDecemberJanuaryFlashWindow_WithDecemberWastePrnAwaitingAcceptance_RendersHtmlStringContentUnescaped()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet,
            prnOrganisationData: WebApiOptions.PrnOrganisationDataType.DecemberWasteAwaitingAcceptance,
            startupUtcTimestampOverride: "2026-12-15T08:00:00Z");
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear + 1);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // The partial passes Content as an HtmlString, so its markup must render raw rather than
        // HTML-encoded — the snapshot captures the accordion body as a real <ul>/<li> subtree.
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenFutureYearSelected_InDecemberJanuaryFlashWindow_WithoutDecemberWastePrnAwaitingAcceptance_HidesDetailsSummaryAccordion()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet,
            prnOrganisationData: WebApiOptions.PrnOrganisationDataType.Default,
            startupUtcTimestampOverride: "2026-12-15T08:00:00Z");
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear + 1);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenShowPrnsOnCdpDisabled_KeepsPackagingAwaitingAcceptanceLinks()
    {
        SetUp(
            showMultiYearObligations: true,
            showPrnsOnCdp: false,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"href=\"{PagePaths.Prns.ShowAwaitingAcceptance}\"");
        content.Should().NotContain("/prns?year=");
    }

    [Test]
    public async Task WhenShowPrnsOnCdpEnabled_AsDirectProducer_LinksToWasteObligationsList()
    {
        SetUp(
            showMultiYearObligations: true,
            showPrnsOnCdp: true,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var organisationId = Guid.Parse("b6f76437-65b6-4ed2-a7d5-c50e9af76201");
        var expectedHref = $"https://understanding-obligations/producer/{organisationId}/prns?year={ComplianceYear}";
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"href=\"{expectedHref}\"");
        content.Should().NotContain($"href=\"{PagePaths.Prns.ShowAwaitingAcceptance}\"");
    }

    [Test]
    public async Task WhenShowPrnsOnCdpEnabled_AsComplianceScheme_LinksToWasteObligationsList()
    {
        SetUp(
            showMultiYearObligations: true,
            showPrnsOnCdp: true,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetComplianceSchemeSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expectedHref = $"https://understanding-obligations/cso/{Accounts.ComplianceSchemeId}/prns?year={ComplianceYear}";
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"href=\"{expectedHref}\"");
        content.Should().NotContain($"href=\"{PagePaths.Prns.ShowAwaitingAcceptance}\"");
    }

    [Test]
    public async Task WhenNoObligations_AndMultiYearEnabled_MeetingObligationsRendersLocalizedContentForSelectedYear()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear + 1);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain($"Your {ComplianceYear + 1} recycling obligations have not been calculated yet. They will be calculated after:");
        content.Should().Contain($"you submit your packaging data for {ComplianceYear}");
        content.Should().Contain("your environmental regulator accepts your H1 and H2 packaging data submissions");
        content.Should().Contain("Until then, you can:");
        content.Should().Contain("acquire packaging waste recycling notes (PRNs) and packaging waste export recycling notes (PERNs)");
        content.Should().Contain("accept your PRNs and PERNs towards your recycling obligations");
        content.Should().Contain("You will see data for PRNs and PERNs you have acquired in the table below.");
        content.Should().NotContain("meeting_obligations_");
        content.Should().NotContain("{0}");
    }

    [Test]
    public async Task WhenNoObligations_AndMultiYearDisabled_DoesNotRenderMeetingObligations()
    {
        SetUp(
            showMultiYearObligations: false,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();
    public async Task WhenCurrentYearSelected_InDecemberJanuaryFlashWindow_WithDecemberWastePrnAwaitingAcceptance_HidesDetailsSummaryAccordion()
    {
        // ShowAccordion requires the selected year to be a *future* Compliance Year - selecting the
        // current year should hide the accordion even though the other two conditions are met.
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet,
            prnOrganisationData: WebApiOptions.PrnOrganisationDataType.DecemberWasteAwaitingAcceptance,
            startupUtcTimestampOverride: "2026-12-15T08:00:00Z");
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("Until then, you can:");
        content.Should().NotContain("have not been calculated yet");
    }

    [Test]
    public async Task WhenNoObligations_AndMultiYearEnabled_InWelsh_MeetingObligationsResolvesAllResources()
    {
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear + 1);
        Context.GetSessionStore().Session.SetString(Language.SessionLanguageKey, Language.Welsh);
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [Test]
    public async Task WhenFutureYearSelected_OutsideDecemberJanuaryFlashWindow_WithDecemberWastePrnAwaitingAcceptance_HidesDetailsSummaryAccordion()
    {
        // ShowAccordion requires being within the Dec/Jan flash window - a future year selection with
        // an otherwise-qualifying PRN should still hide the accordion outside that window (e.g. June).
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet,
            prnOrganisationData: WebApiOptions.PrnOrganisationDataType.DecemberWasteAwaitingAcceptance,
            startupUtcTimestampOverride: "2026-06-15T08:00:00Z");
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: ComplianceYear + 1);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // The layout hard-codes lang="en", so use the language toggle (which offers English when Welsh is active).
        content.Should().Contain("culture?culture=en");
        content.Should().NotContain("meeting_obligations_");
        content.Should().NotContain("{0}");

        // Structural check that the partial rendered: its two bullet lists are the only elements carrying this margin class here.
        System.Text.RegularExpressions.Regex
            .Matches(content, "<ul class=\"govuk-list govuk-list--bullet govuk-!-margin-left-2\">")
            .Should().HaveCount(2);
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes();
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    private void SetUp(
        bool showMultiYearObligations,
        WebApiOptions.ObligationDataType obligationData,
        WebApiOptions.PrnOrganisationDataType prnOrganisationData = WebApiOptions.PrnOrganisationDataType.Default,
        string? startupUtcTimestampOverride = null,
        bool showPrnsOnCdp = false)
    {
        var additionalConfig = new Dictionary<string, string?>
        {
            { "FeatureManagement:ShowMultiYearObligations", showMultiYearObligations.ToString().ToLowerInvariant() },
            { "FeatureManagement:CsocEnabled", "false" },
            { "FeatureManagement:ShowPrnsOnCdp", showPrnsOnCdp.ToString().ToLowerInvariant() }
        };

        if (startupUtcTimestampOverride is not null)
        {
            additionalConfig["StartupUtcTimestampOverride"] = startupUtcTimestampOverride;
        }

        Context.SetUp(
            overrideSession: true,
            additionalConfig: additionalConfig,
            new WebApiOptions
            {
                ObligationData = obligationData,
                PrnOrganisationData = prnOrganisationData,
                ServiceRole = ServiceRoleConstants.Approved
            });
    }

    private void SetProducerSession(int? selectedObligationYear = null)
    {
        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(
            nameof(FrontendSchemeRegistrationSession),
            Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    ServiceRole = ServiceRoleConstants.Approved,
                    Organisations =
                    [
                        new Organisation
                        {
                            Id = Guid.Parse("b6f76437-65b6-4ed2-a7d5-c50e9af76201"),
                            OrganisationRole = "Producer",
                            Name = "Test Organisation",
                            NationId = 1
                        }
                    ]
                },
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto
                    {
                        Id = Accounts.ComplianceSchemeId
                    }
                },
                PrnSession = new PrnSession
                {
                    SelectedObligationYear = selectedObligationYear
                }
            })));
    }

    private void SetComplianceSchemeSession(int? selectedObligationYear = null)
    {
        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(
            nameof(FrontendSchemeRegistrationSession),
            Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    ServiceRole = ServiceRoleConstants.Approved,
                    Organisations =
                    [
                        new Organisation
                        {
                            Id = Guid.Parse("b6f76437-65b6-4ed2-a7d5-c50e9af76201"),
                            OrganisationRole = "Compliance Scheme",
                            Name = "Test Organisation",
                            NationId = 1
                        }
                    ]
                },
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto
                    {
                        Id = Accounts.ComplianceSchemeId,
                        Name = "Test Scheme"
                    }
                },
                PrnSession = new PrnSession
                {
                    SelectedObligationYear = selectedObligationYear
                }
            })));
    }
}
