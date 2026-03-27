namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using AngleSharp.Dom;
using Application.DTOs.ComplianceScheme;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using FluentAssertions;
using Infrastructure;
using MockServer.WebApi;
using Sessions;
using VerifyTests.AngleSharp;

public class CsocTests
{
    private ComponentTestContext Context { get; } = new();
    
    [TestCase("/report-data/home-compliance-scheme", Language.English, true)]
    [TestCase("/report-data/home-compliance-scheme", Language.Welsh, true)]
    [TestCase("/report-data/home-compliance-scheme", Language.English, false)]
    [TestCase("/report-data/home-compliance-scheme", Language.Welsh, false)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.English, true)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.Welsh, true)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.English, false)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.Welsh, false)]
    public async Task WhenCsocEnabledOrDisabled_ShouldLocalizeAsExpected(string path, string language, bool csocEnabled)
    {
        Context.SetUp(overrideSession: true, additionalConfig: new Dictionary<string, string?>
        {
            { "FeatureManagement:CsocEnabled", csocEnabled.ToString().ToLower() }
        });
        
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));
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
                            OrganisationRole = "Producer"
                        }
                    ]
                },
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto
                    {
                        Id = Accounts.ComplianceSchemeId
                    }
                }
            })));
        
        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .PrettyPrintHtml(nodes =>
            {
                foreach (var node in nodes.QuerySelectorAll("button[name=\"selectedComplianceSchemeId\"]"))
                    node.Attributes.GetNamedItem("value").Value = "[Scrubbed]";
            })
            .UseParameters(path, language, csocEnabled);
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }
}