namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
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
    
    [TestCase("/report-data/view-awaiting-acceptance-alt")]
    [TestCase("/report-data/view-awaiting-acceptance")]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000001")]
    [TestCase("/report-data/accept-prn/00000000-0000-0000-0000-000000000003")]
    [TestCase("/report-data/accepted-prn/00000000-0000-0000-0000-000000000005")]
    [TestCase("/report-data/accept-bulk")]
    [TestCase("/report-data/accepted-prns")]
    [TestCase("/report-data/download-prns-csv")]
    public async Task WhenPrnsArePresent_ShouldLocalizeAsExpected(string path)
    {
        await Context.Client.AuthenticateDefaultUser();

        if (Session.TryGetValue(path, out var value))
            value(Context.GetSessionStore());

        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        if (path.EndsWith("csv"))
            await Verify(content, VerifyCsv.Extension).UseParameters(path).DontScrubDateTimes();
        else
            await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings).UseParameters(path);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}