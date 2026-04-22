namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using AngleSharp.Dom;
using Application.DTOs.ComplianceScheme;
using Application.Enums;
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
    
    [TestCase("/report-data/home-compliance-scheme", Language.English, true, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/home-compliance-scheme", Language.English, true, ServiceRoleConstants.Delegated)]
    [TestCase("/report-data/home-compliance-scheme", Language.Welsh, true, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/home-compliance-scheme", Language.English, false, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/home-compliance-scheme", Language.Welsh, false, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.English, true, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.English, true, ServiceRoleConstants.Delegated)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.Welsh, true, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.English, false, ServiceRoleConstants.Approved)]
    [TestCase("/report-data/manage-your-recycling-obligations", Language.Welsh, false, ServiceRoleConstants.Approved)]
    public async Task WhenCsocEnabledOrDisabled_ShouldLocalizeAsExpected(string path, string language, bool csocEnabled, string serviceRole)
    {
        SetUp(csocEnabled);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));
        SetSession(sessionStore, serviceRole);
        
        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubComplianceSchemeId()
            .ScrubCommonHtmlNodes()
            .UseParameters(path, language, csocEnabled, serviceRole.Replace(" ", ""));
    }
    
    [TestCase("/report-data/home-compliance-scheme")]
    [TestCase("/report-data/manage-your-recycling-obligations")]
    public async Task WhenBasicUser_ShouldHidePrivilegedContent(string path)
    {
        SetUp(csocEnabled: true);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        SetSession(sessionStore, ServiceRoleConstants.Basic);
        
        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubComplianceSchemeId()
            .ScrubCommonHtmlNodes()
            .UseParameters(path);
    }
    
    [Test]
    public async Task WhenNoObligationData_ShouldHideSubmissionTile()
    {
        SetUp(csocEnabled: true, obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        SetSession(sessionStore, ServiceRoleConstants.Basic);
        
        var response = await Context.Client.GetAsync("/report-data/manage-your-recycling-obligations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubComplianceSchemeId()
            .ScrubCommonHtmlNodes();
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    private void SetUp(
        bool csocEnabled, 
        WebApiOptions.ObligationDataType obligationData = WebApiOptions.ObligationDataType.Mixed)
    {
        Context.SetUp(overrideSession: true, additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:CsocEnabled", csocEnabled.ToString().ToLower() }
            },
            new WebApiOptions
            {
                ObligationData = obligationData
            });
    }

    private static void SetSession(SessionStore sessionStore, string serviceRole)
    {
        sessionStore.Session.Set(nameof(FrontendSchemeRegistrationSession),
            Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    ServiceRole = serviceRole,
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
    }
}

public static class CsocTestsExtensions
{
    public static SettingsTask ScrubComplianceSchemeId(this SettingsTask settings)
    {
        settings.PrettyPrintHtml(nodes =>
        {
            foreach (var node in nodes.QuerySelectorAll("button[name=\"selectedComplianceSchemeId\"]"))
                node.Attributes.GetNamedItem("value").Value = "[Scrubbed]";
        });
        
        return settings;
    }
}