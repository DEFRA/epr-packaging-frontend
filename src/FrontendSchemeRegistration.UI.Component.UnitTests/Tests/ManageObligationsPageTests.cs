namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Application.DTOs.ComplianceScheme;
using Application.Enums;
using Application.Extensions;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using FluentAssertions;
using Infrastructure;
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
        content.Should().Contain($"Your {ComplianceYear} recycling obligations will be calculated after:");
        content.Should().Contain($"you submit your packaging data for {ComplianceYear - 1}");
        content.Should().Contain("the regulator accepts your H1 and H2 packaging data submissions");
        content.Should().NotContain("You can start acquiring and accepting PRNs and PERNs");
        content.Should().NotContain("Your recycling obligations will be calculated after:");
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
        content.Should().Contain("Your recycling obligations will be calculated after:");
        content.Should().Contain($"you submit your packaging data for {ComplianceYear - 1}");
        content.Should().Contain("the regulator accepts your data submissions");
        content.Should().Contain("You can start acquiring and accepting PRNs and PERNs to meet your recycling obligations.");
        content.Should().NotContain("H1 and H2");
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
        content.Should().Contain(
            $"Acquire and accept PRNs and PERNs until your recycling obligations are fully met. Select a material for information on how the data was calculated and view your progress towards meeting your {ComplianceYear} recycling obligations.");
        content.Should().NotContain("will be calculated after:");
        content.Should().NotContain("H1 and H2");
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
        content.Should().Contain("govuk-details");
        content.Should().Contain("Lorem ipsum dolor sit amet");
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

        // The partial passes Content as an HtmlString, so its markup must render raw rather than HTML-encoded.
        content.Should().Contain("<ul><li>prīmus</li><li>secundus</li><li>tertius</li></ul>");
        content.Should().NotContain("&lt;ul&gt;");
        content.Should().NotContain("&lt;li&gt;");
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
        content.Should().NotContain("govuk-details");
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
        string? startupUtcTimestampOverride = null)
    {
        var additionalConfig = new Dictionary<string, string?>
        {
            { "FeatureManagement:ShowMultiYearObligations", showMultiYearObligations.ToString().ToLowerInvariant() },
            { "FeatureManagement:CsocEnabled", "false" }
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
}
