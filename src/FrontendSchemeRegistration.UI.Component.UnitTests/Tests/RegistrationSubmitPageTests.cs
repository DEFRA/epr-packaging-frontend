namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Extensions;
using FluentAssertions;
using Infrastructure;
using MockServer.WebApi;
using NUnit.Framework;

/// <summary>
/// The session is shared by every tab in the browser, so these tests open several registrations one after another (as
/// separate tabs or the back button would) and check each submission is sent for the registration its page was opened for.
/// </summary>
public class RegistrationSubmitPageTests
{
    private const string Producer = "Producer";
    private const string ComplianceScheme = "Compliance Scheme";

    private const string ReferenceA = "PEPR15497725P1";
    private const string ReferenceB = "PEPR15497726P1";

    private static readonly Guid SubmissionA = new("a5a5a5a5-0000-4000-8000-000000002025");
    private static readonly Guid SubmissionB = new("b6b6b6b6-0000-4000-8000-000000002026");

    private ComponentTestContext Context { get; } = new();

    private string? _organisationRole;
    private string? _registrationJourney;

    [TearDown]
    public void TearDown()
    {
        Context.Dispose();
    }

    [TestCase(Producer, null)]
    [TestCase(ComplianceScheme, "CsoLargeProducer")]
    public async Task WhenSubmitPagesAreOpenForTwoRegistrationsInTwoTabs_EachSubmissionIsForItsOwnRegistration(string organisationRole, string? registrationJourney)
    {
        await SetUp(organisationRole, registrationJourney);

        // Tab one opens the 2025 registration, then tab two opens the 2026 registration, which the session now holds
        var submitFormA = await OpenSubmitPage(await OpenTaskList(2025));
        var submitFormB = await OpenSubmitPage(await OpenTaskList(2026));

        var responseA = await Submit(submitFormA, "Tab one");
        responseA.StatusCode.Should().Be(HttpStatusCode.Redirect);
        responseA.Headers.Location!.ToString().Should().Contain($"submissionId={SubmissionA}");
        AssertSubmitted(SubmissionA, ReferenceA, "Tab one");

        await Submit(submitFormB, "Tab two");
        AssertSubmitted(SubmissionB, ReferenceB, "Tab two");

        var confirmationA = await Context.Client.GetAsync(responseA.Headers.Location!.ToString());
        confirmationA.StatusCode.Should().Be(HttpStatusCode.OK);
        (await confirmationA.Content.ReadAsStringAsync()).Should().Contain(ReferenceA).And.NotContain(ReferenceB);
    }

    [TestCase(Producer, null)]
    [TestCase(ComplianceScheme, "CsoLargeProducer")]
    public async Task WhenBackButtonReturnsToAnEarlierRegistration_SubmittingSubmitsThatRegistration(string organisationRole, string? registrationJourney)
    {
        await SetUp(organisationRole, registrationJourney);

        var submitPageA = await OpenTaskList(2025);
        var submitFormA = await OpenSubmitPage(submitPageA);
        await OpenSubmitPage(await OpenTaskList(2026));

        // Going back shows registration A's submit page again, while the session still holds registration B
        var submitFormAfterBack = await OpenSubmitPage(submitPageA);
        submitFormAfterBack.Should().Be(submitFormA);

        await Submit(submitFormAfterBack, "Back again");

        AssertSubmitted(SubmissionA, ReferenceA, "Back again");
        Context.GetMockApiRequests("POST", SubmitRegistrationApplicationPath(SubmissionB)).Should().BeEmpty();
    }

    [TestCase("/report-data/additional-information?registrationyear=2025")]
    [TestCase("/report-data/additional-information?registrationyear=2025&submissionId=0f0f0f0f-0000-4000-8000-000000000000")]
    [TestCase("/report-data/submit-registration-request?registrationyear=2025")]
    public async Task WhenSubmissionIsMissingOrUnknown_RedirectsToAccountHome(string url)
    {
        await SetUp(Producer, null);

        var response = await Context.Client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/report-data/");
    }

    private async Task SetUp(string organisationRole, string? registrationJourney)
    {
        _organisationRole = organisationRole;
        _registrationJourney = registrationJourney;

        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                // The payment service fee snapshot isn't stubbed, so fees come from the registration application details
                { "FeatureManagement:EnableRegistrationFeeParametersViaPaymentService", "false" }
            },
            webApiOptions: new WebApiOptions
            {
                OrganisationRole = organisationRole,
                RegistrationApplications =
                [
                    new RegistrationApplicationStub(SubmissionA, 2025, ReferenceA, registrationJourney),
                    new RegistrationApplicationStub(SubmissionB, 2026, ReferenceB, registrationJourney)
                ]
            });

        await Context.Client.AuthenticateDefaultUser();

        if (organisationRole == ComplianceScheme)
        {
            // Account home selects the operator's compliance scheme
            var response = await Context.Client.GetAsync("/report-data/home-compliance-scheme");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    private async Task<string> OpenTaskList(int registrationYear)
    {
        var url = $"/report-data/registration-task-list?registrationyear={registrationYear}";
        if (_registrationJourney is not null)
        {
            url += $"&registrationjourney={_registrationJourney}";
        }

        var response = await Context.Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await GetAttribute(response, "a[href*='additional-information']", "href");
    }

    private async Task<string> OpenSubmitPage(string submitPageUrl)
    {
        var response = await Context.Client.GetAsync(submitPageUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await GetAttribute(response, "form[action*='additional-information']", "action");
    }

    private Task<HttpResponseMessage> Submit(string formAction, string additionalInformation) =>
        Context.Client.PostAsync(formAction, new Dictionary<string, string> { ["AdditionalInformationText"] = additionalInformation });

    private void AssertSubmitted(Guid submissionId, string applicationReferenceNumber, string comments)
    {
        var requests = Context.GetMockApiRequests("POST", SubmitRegistrationApplicationPath(submissionId));
        requests.Should().ContainSingle();

        using var payload = JsonDocument.Parse(requests[0].Body!);
        payload.RootElement.GetProperty("applicationReferenceNumber").GetString().Should().Be(applicationReferenceNumber);
        payload.RootElement.GetProperty("comments").GetString().Should().Be(comments);
        payload.RootElement.GetProperty("isResubmission").GetBoolean().Should().BeFalse();
        payload.RootElement.GetProperty("registrationJourney").GetString().Should().Be(_registrationJourney);

        var complianceSchemeId = payload.RootElement.GetProperty("complianceSchemeId");
        if (_organisationRole == ComplianceScheme)
        {
            // Account home selects the operator's only compliance scheme
            complianceSchemeId.GetGuid().Should().Be(Accounts.ComplianceSchemeId);
        }
        else
        {
            complianceSchemeId.ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    private static string SubmitRegistrationApplicationPath(Guid submissionId) =>
        $"/api/v1/submissions/{submissionId}/submit-registration-application";

    private static async Task<string> GetAttribute(HttpResponseMessage response, string selector, string attribute)
    {
        var document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync());
        var value = document.QuerySelector(selector)?.GetAttribute(attribute);
        value.Should().NotBeNullOrEmpty($"the page should contain {selector}");

        return value!;
    }
}
