namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Constants;
using Extensions;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;
using Sessions;

public class ObligationsTests
{
    private ComponentTestContext Context { get; } = new();
    private Dictionary<string, Action<SessionStore>> Session { get; } = new();

    [SetUp]
    public void SetUp()
    {
        Context.SetUp(overrideSession: true);
        
        Session.TryAdd("/report-data/accept-bulk", sessionStore =>
        {
            sessionStore.Session.Set(nameof(FrontendSchemeRegistrationSession),
                Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
                {
                    PrnSession = new PrnSession
                    {
                        SelectedPrnIds =
                        [
                            new Guid("00000000-0000-0000-0000-000000000001"),
                            new Guid("00000000-0000-0000-0000-000000000003"),
                        ]
                    }
                })));
        });
        Session.TryAdd("/report-data/accepted-prns", sessionStore =>
        {
            sessionStore.Session.Set(nameof(FrontendSchemeRegistrationSession),
                Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
                {
                    PrnSession = new PrnSession
                    {
                        SelectedPrnIds =
                        [
                            new Guid("00000000-0000-0000-0000-000000000005"),
                            new Guid("00000000-0000-0000-0000-000000000006"),
                        ]
                    }
                })));
        });
    }
    
    [TestCase("/report-data/view-awaiting-acceptance-alt", Language.English)]
    [TestCase("/report-data/view-awaiting-acceptance-alt", Language.Welsh)]
    [TestCase("/report-data/view-awaiting-acceptance", Language.English)]
    [TestCase("/report-data/view-awaiting-acceptance", Language.Welsh)]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000001", Language.English)]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000001", Language.Welsh)]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000003", Language.English)]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000003", Language.Welsh)]
    [TestCase("/report-data/accepted-prn/00000000-0000-0000-0000-000000000005", Language.English)]
    [TestCase("/report-data/accepted-prn/00000000-0000-0000-0000-000000000005", Language.Welsh)]
    [TestCase("/report-data/accept-bulk", Language.English)]
    [TestCase("/report-data/accept-bulk", Language.Welsh)]
    [TestCase("/report-data/accepted-prns", Language.English)]
    [TestCase("/report-data/accepted-prns", Language.Welsh)]
    [TestCase("/report-data/download-prns-csv", Language.English)]
    [TestCase("/report-data/download-prns-csv", Language.Welsh)]
    [TestCase("/report-data/selected-prn/00000000-0000-0000-0000-000000000001", Language.English)]
    [TestCase("/report-data/selected-prn/00000000-0000-0000-0000-000000000001", Language.Welsh)]
    [TestCase("/report-data/selected-prn/00000000-0000-0000-0000-000000000002", Language.English)]
    [TestCase("/report-data/selected-prn/00000000-0000-0000-0000-000000000002", Language.Welsh)]
    public async Task WhenPrnsArePresent_ShouldLocalizeAsExpected(string path, string language)
    {
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        if (Session.TryGetValue(path, out var value))
            value(sessionStore);
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));

        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        if (path.EndsWith("csv"))
            await Verify(content, VerifyCsv.Extension).UseParameters(path, language).DontScrubDateTimes();
        else
            await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
                .ScrubCommonHtmlNodes()
                .UseParameters(path, language);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}