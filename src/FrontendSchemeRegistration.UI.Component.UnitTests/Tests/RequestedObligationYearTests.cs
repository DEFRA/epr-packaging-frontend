namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Application.Constants;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using FluentAssertions;
using Infrastructure;
using Sessions;

public class RequestedObligationYearTests
{
    private const string BasePath = "/report-data/";
    private const string CertificatePath = BasePath + PagePaths.Prns.ComplianceCertificate;
    private const string PrnId = "00000000-0000-0000-0000-000000000001";
    private const string AcceptedPrnId = "00000000-0000-0000-0000-000000000005";

    private ComponentTestContext Context { get; } = new();

    public static IEnumerable<string> RedirectingPages()
    {
        yield return PagePaths.Prns.ObligationsHome;
        yield return PagePaths.Prns.ChooseYear;
        yield return PagePaths.Prns.ObligationPerMaterial + "/plastic";
        yield return PagePaths.Prns.Search;
        yield return PagePaths.Prns.ShowAwaitingAcceptance;
        yield return PagePaths.Prns.ShowSelected + "/" + PrnId;
        yield return PagePaths.Prns.AskToChooseAcceptanceYear + "/" + PrnId;
        yield return PagePaths.Prns.AskToAccept + "/" + PrnId;
        yield return PagePaths.Prns.Accepted + "/" + AcceptedPrnId;
        yield return PagePaths.Prns.AskToAcceptMany;
        yield return PagePaths.Prns.AcceptedMany;
        yield return PagePaths.Prns.AskToReject + "/" + PrnId;
        yield return PagePaths.Prns.Rejected + "/" + AcceptedPrnId;
        yield return PagePaths.Prns.ConfirmAccept;
        yield return PagePaths.Prns.BeforeAskToAcceptMany;
        yield return PagePaths.Prns.ConfirmAcceptMany;
        yield return PagePaths.Prns.ConfirmReject;
    }

    [TestCaseSource(nameof(RedirectingPages))]
    public async Task Get_WhenYearQueryIs2025_RedirectsToComplianceCertificate(string page)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();
        SetSession(Context.GetSessionStore(), selectedObligationYear: null);

        var response = await Context.Client.GetAsync($"{BasePath}{page}?year=2025");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(CertificatePath);
    }

    [TestCase(2024)]
    [TestCase(2000)]
    public async Task Get_WhenYearQueryIsBefore2025_RedirectsToComplianceCertificate(int year)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();
        SetSession(Context.GetSessionStore(), selectedObligationYear: null);

        var response = await Context.Client.GetAsync($"{BasePath}{PagePaths.Prns.ObligationsHome}?year={year}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(CertificatePath);
    }

    [Test]
    public async Task Get_WhenYearQueryIs2025_SelectsTheCertificateYearForTheSession()
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();
        SetSession(Context.GetSessionStore(), selectedObligationYear: 2026);

        await Context.Client.GetAsync($"{BasePath}{PagePaths.Prns.Search}?year=2025");
        var certificateResponse = await Context.Client.GetAsync(CertificatePath);

        certificateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestCaseSource(nameof(RedirectingPages))]
    public async Task Get_WhenYearQueryIs2026_DoesNotRedirectToComplianceCertificate(string page)
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();
        SetSession(Context.GetSessionStore(), selectedObligationYear: null);

        var response = await Context.Client.GetAsync($"{BasePath}{page}?year=2026");

        response.Headers.Location?.OriginalString.Should().NotBe(CertificatePath);
    }

    [TestCaseSource(nameof(RedirectingPages))]
    public async Task Get_WhenMultiYearObligationsDisabled_DoesNotRedirectToComplianceCertificate(string page)
    {
        SetUp(showMultiYearObligations: false);
        await Context.Client.AuthenticateDefaultUser();
        SetSession(Context.GetSessionStore(), selectedObligationYear: null);

        var response = await Context.Client.GetAsync($"{BasePath}{page}?year=2025");

        response.Headers.Location?.OriginalString.Should().NotBe(CertificatePath);
    }

    [Test]
    public async Task Get_WhenYearQueryIs2025_OnCsvDownload_DoesNotRedirect()
    {
        SetUp(showMultiYearObligations: true);
        await Context.Client.AuthenticateDefaultUser();
        SetSession(Context.GetSessionStore(), selectedObligationYear: null);

        var response = await Context.Client.GetAsync($"{BasePath}{PagePaths.Prns.DownloadAllPRNsAndPERNs}?year=2025");

        response.Headers.Location.Should().BeNull();
        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
    }

    private void SetUp(bool showMultiYearObligations)
    {
        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:ShowMultiYearObligations", showMultiYearObligations.ToString().ToLower() },
                { "FeatureManagement:ShowDecemberWaste", "true" }
            });
    }

    private static void SetSession(SessionStore sessionStore, int? selectedObligationYear)
    {
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(Language.English));
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
                            OrganisationRole = OrganisationRoles.Producer,
                            NationId = null
                        }
                    ]
                },
                PrnSession = new PrnSession
                {
                    SelectedObligationYear = selectedObligationYear
                },
                RegistrationSession = new RegistrationSession()
            })));
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}
