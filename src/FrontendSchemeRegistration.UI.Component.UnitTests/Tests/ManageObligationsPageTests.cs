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

    private ComponentTestContext Context { get; } = new();

    [Test]
    public async Task WhenNoObligations_AndMultiYearEnabled_ShowsAlternativeContent()
    {
        var complianceYear = DateTimeOffset.UtcNow.GetComplianceYear();
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: complianceYear);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Your {complianceYear} recycling obligations will be calculated after:");
        content.Should().Contain($"you submit your packaging data for {complianceYear - 1}");
        content.Should().Contain("the regulator accepts your H1 and H2 packaging data submissions");
        content.Should().NotContain("You can start acquiring and accepting PRNs and PERNs");
        content.Should().NotContain("Your recycling obligations will be calculated after:");
    }

    [Test]
    public async Task WhenNoObligations_AndMultiYearDisabled_ShowsLegacyContent()
    {
        var complianceYear = DateTimeOffset.UtcNow.GetComplianceYear();
        SetUp(
            showMultiYearObligations: false,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession();

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Your recycling obligations will be calculated after:");
        content.Should().Contain($"you submit your packaging data for {complianceYear - 1}");
        content.Should().Contain("the regulator accepts your data submissions");
        content.Should().Contain("You can start acquiring and accepting PRNs and PERNs to meet your recycling obligations.");
        content.Should().NotContain("H1 and H2");
        content.Should().NotContain($"Your {complianceYear} recycling obligations will be calculated after:");
    }

    [Test]
    public async Task WhenObligationsPresent_AndMultiYearEnabled_ShowsExistingAdvisoryText()
    {
        var complianceYear = DateTimeOffset.UtcNow.GetComplianceYear();
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.Mixed);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: complianceYear);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(
            $"Acquire and accept PRNs and PERNs until your recycling obligations are fully met. Select a material for information on how the data was calculated and view your progress towards meeting your {complianceYear} recycling obligations.");
        content.Should().NotContain("will be calculated after:");
        content.Should().NotContain("H1 and H2");
    }

    [Test]
    public async Task WhenMultiYearEnabled_AndFutureYearSelected_ShowsSelectedYearInHeadingAndAlternativeContent()
    {
        var complianceYear = DateTimeOffset.UtcNow.GetComplianceYear();
        var futureYear = complianceYear + 1;
        SetUp(
            showMultiYearObligations: true,
            obligationData: WebApiOptions.ObligationDataType.NoDataYet);
        await Context.Client.AuthenticateDefaultUser();
        SetProducerSession(selectedObligationYear: futureYear);

        var response = await Context.Client.GetAsync(ObligationsHomePath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Manage your {futureYear} recycling obligations");
        content.Should().Contain($"Your {futureYear} recycling obligations will be calculated after:");
        content.Should().Contain($"you submit your packaging data for {complianceYear}");
        content.Should().Contain("the regulator accepts your H1 and H2 packaging data submissions");
    }

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    private void SetUp(
        bool showMultiYearObligations,
        WebApiOptions.ObligationDataType obligationData)
    {
        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:ShowMultiYearObligations", showMultiYearObligations.ToString().ToLowerInvariant() },
                { "FeatureManagement:CsocEnabled", "false" }
            },
            new WebApiOptions
            {
                ObligationData = obligationData,
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
