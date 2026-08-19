namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using FluentAssertions;
using Infrastructure;
using Sessions;

public class ComplianceCertificateTests
{
    private const string Path = "/report-data/certificate-of-compliance-record";

    private ComponentTestContext Context { get; } = new();

    [TestCase(Language.English)]
    [TestCase(Language.Welsh)]
    public async Task Get_WhenMultiYearObligationsEnabledAndSelectedYearIs2025_ReturnsComplianceCertificatePage(string language)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));
        SetSession(sessionStore, selectedObligationYear: 2025);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<title>Your certificate of compliance record for 2025");
        content.Should().Contain("Your certificate of compliance record for 2025");
        content.Should().Contain("We can only show certificate of compliance records for certificates that were processed by the ‘Report packaging data’ service.");
        content.Should().Contain("We cannot show certificates of compliance for 2025, because they were processed outside the service.");
        content.Should().Contain("If you have any queries, contact the Environment Agency:");
        content.Should().Contain("mailto:packagingproducers@environment-agency.gov.uk");
        content.Should().Contain("You can view your PRNs and PERNs for 2025 below.");
        content.Should().Contain("Your PRNs and PERNs");
        content.Should().Contain("href=\"view-awaiting-acceptance\"");
        content.Should().Contain("Search PRNs and PERNs");
        content.Should().Contain("href=\"download-prns-csv\"");
        content.Should().Contain("Download a list of your PRNs and PERNs for 2025 (CSV)");
    }

    [TestCase(Language.English)]
    [TestCase(Language.Welsh)]
    public async Task Get_WhenLoggedInAsDirectProducer_ShowsCertificateWording(string language)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));
        SetSession(sessionStore, selectedObligationYear: 2025, organisationRole: OrganisationRoles.Producer);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<title>Your certificate of compliance record for 2025");
        content.Should().Contain("Your certificate of compliance record for 2025");
        content.Should().Contain("We can only show certificate of compliance records for certificates that were processed by the ‘Report packaging data’ service.");
        content.Should().Contain("We cannot show certificates of compliance for 2025, because they were processed outside the service.");
        content.Should().NotContain("Your statement of compliance record for 2025");
    }

    [TestCase(Language.English)]
    [TestCase(Language.Welsh)]
    public async Task Get_WhenLoggedInAsComplianceScheme_ShowsStatementWording(string language)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));
        SetSession(sessionStore, selectedObligationYear: 2025, organisationRole: OrganisationRoles.ComplianceScheme);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<title>Your statement of compliance record for 2025");
        content.Should().Contain("Your statement of compliance record for 2025");
        content.Should().Contain("We can only show statement of compliance records for statements that were processed by the ‘Report packaging data’ service.");
        content.Should().Contain("We cannot show statements of compliance for 2025, because they were processed outside the service.");
        content.Should().NotContain("Your certificate of compliance record for 2025");
    }

    [TestCase(null, "Environment Agency", "packagingproducers@environment-agency.gov.uk")]
    [TestCase(1, "Environment Agency", "packagingproducers@environment-agency.gov.uk")]
    [TestCase(2, "Northern Ireland Environment Agency", "packaging@daera-ni.gov.uk")]
    [TestCase(3, "Scottish Environment Protection Agency", "producer.responsibility@sepa.org.uk")]
    [TestCase(4, "Natural Resources Wales", "packaging@naturalresourceswales.gov.uk")]
    public async Task Get_RendersNationSpecificEnvironmentAgencyContactDetails(int? nationId, string expectedAgencyName, string expectedEmail)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        SetSession(sessionStore, selectedObligationYear: 2025, nationId: nationId);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"If you have any queries, contact the {expectedAgencyName}:");
        content.Should().Contain($"mailto:{expectedEmail}");
    }

    [Test]
    public async Task Get_RendersBackLinkToChooseYearPage()
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        SetSession(sessionStore, selectedObligationYear: 2025);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"<a href=\"/report-data/{PagePaths.Prns.ChooseYear}\" class=\"govuk-back-link\"");
    }

    [Test]
    public async Task Get_WhenMultiYearObligationsFeatureDisabled_ReturnsNotFound()
    {
        SetUp(showMultiYearObligations: false);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        SetSession(sessionStore, selectedObligationYear: 2025);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestCase(null)]
    [TestCase(2024)]
    [TestCase(2026)]
    public async Task Get_WhenSelectedObligationYearIsNot2025_ReturnsNotFound(int? selectedObligationYear)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        SetSession(sessionStore, selectedObligationYear);

        var response = await Context.Client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetUp(bool showMultiYearObligations)
    {
        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:ShowMultiYearObligations", showMultiYearObligations.ToString().ToLower() }
            });
    }

    private static void SetSession(SessionStore sessionStore, int? selectedObligationYear, string organisationRole = OrganisationRoles.Producer, int? nationId = null)
    {
        sessionStore.Session.Set(nameof(FrontendSchemeRegistrationSession),
            Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    ServiceRole = "Approved Person",
                    Organisations =
                    [
                        new Organisation
                        {
                            Id = Guid.NewGuid(),
                            OrganisationRole = organisationRole,
                            NationId = nationId
                        }
                    ]
                },
                PrnSession = new PrnSession
                {
                    SelectedObligationYear = selectedObligationYear
                },
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = organisationRole == OrganisationRoles.ComplianceScheme
                        ? new ComplianceSchemeDto { Id = Guid.NewGuid() }
                        : null
                }
            })));
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}
