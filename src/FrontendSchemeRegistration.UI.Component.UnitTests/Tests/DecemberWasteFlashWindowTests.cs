namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Constants;
using Extensions;
using FluentAssertions;
using Infrastructure;
using MockServer.WebApi;
using NUnit.Framework;
using Sessions;

/// <summary>
///     December Waste PRN pages rendered inside the Dec/Jan flash window (clock set to 15 Dec 2026),
///     with the ShowDecemberWaste and ShowMultiYearObligations feature flags on and off.
/// </summary>
public class DecemberWasteFlashWindowTests
{
    private const string InFlashWindow = "2026-12-15T08:00:00Z";
    private const string SelectMultiplePath = "/report-data/view-awaiting-acceptance-alt";
    private const string AcceptMultiplePath = "/report-data/accept-bulk";
    private const string AcceptMultiplePassThroughPath = "/report-data/accept-bulk-passthrough";
    private const string StandardPrnId = "00000000-0000-0000-0000-000000000202";
    private const string FlashPrnId = "00000000-0000-0000-0000-000000000201";
    private const string SelectSinglePath = $"/report-data/selected-prn/{FlashPrnId}";
    private const string ChooseAcceptanceYearPath = $"/report-data/choose-acceptance-year/{FlashPrnId}";
    private const string AcceptSinglePath = $"/report-data/accept-prn/{FlashPrnId}";

    private ComponentTestContext Context { get; } = new();

    [TearDown]
    public void TearDown() => Context.Dispose();

    [Test]
    public async Task SelectMultiplePrns_WhenDecemberWasteEnabled_FlashPrnIsLinkNotCheckbox()
    {
        await SetUp(decemberWaste: true, multiYear: true, WebApiOptions.PrnSearchDataType.DecemberWasteInFlashWindow);

        var content = await GetOk(SelectMultiplePath);

        content.Should().Contain("december-waste-flash-row");
        content.Should().Contain("Can be accepted towards 2026 or 2027 recycling obligations");
        content.Should().Contain($"href=\"/report-data/selected-prn/{FlashPrnId}\"");
        CountCheckboxes(content).Should().Be(1, "only the standard PRN can be selected");
        AcceptButtonIsVisible(content).Should().BeTrue();
    }

    [Test]
    public async Task SelectMultiplePrns_WhenDecemberWasteEnabled_AndAllPrnsInFlashWindow_HidesAcceptSelectedButton()
    {
        await SetUp(decemberWaste: true, multiYear: true, WebApiOptions.PrnSearchDataType.AllDecemberWasteInFlashWindow);

        var content = await GetOk(SelectMultiplePath);

        CountCheckboxes(content).Should().Be(0);
        AcceptButtonIsVisible(content).Should().BeFalse();
    }

    [Test]
    public async Task SelectMultiplePrns_WhenDecemberWasteDisabled_AndMultiYearEnabled_YearChoicePrnsAreLinksNotCheckboxes()
    {
        await SetUp(decemberWaste: false, multiYear: true, WebApiOptions.PrnSearchDataType.AllDecemberWasteInFlashWindow);

        var content = await GetOk(SelectMultiplePath);

        CountCheckboxes(content).Should().Be(0, "PRNs with a choice of year must be accepted individually");
        content.Should().Contain($"href=\"/report-data/selected-prn/{FlashPrnId}\"");
        content.Should().NotContain("december-waste-flash-row");
        AcceptButtonIsVisible(content).Should().BeFalse();
    }

    [Test]
    public async Task SelectMultiplePrns_WhenNothingSelectableOnPage_AcceptButtonIsHiddenButRevealedForSelectionsRetainedFromOtherPages()
    {
        // A later page holding only PRNs that need a year choosing: nothing can be ticked here, but selections
        // ticked on earlier pages are kept in sessionStorage and must still be submittable.
        await SetUp(decemberWaste: false, multiYear: true, WebApiOptions.PrnSearchDataType.AllDecemberWasteInFlashWindow);

        var content = await GetOk(SelectMultiplePath);

        CountCheckboxes(content).Should().Be(0);
        AcceptButtonIsVisible(content).Should().BeFalse("nothing on this page is selectable");
        content.Should().Contain("id=\"acceptSelectedPrns\"", "the button stays in the page so it can be revealed");
        content.Should().Contain("acceptButton.hidden = false");
        content.Should().Contain("sessionStorage.key(i).startsWith(\"chkBoxSelectPrn_\")");
    }

    [Test]
    public async Task SelectMultiplePrns_WhenDecemberWasteDisabled_AndMultiYearDisabled_AllPrnsHaveCheckboxesAndNoFlash()
    {
        await SetUp(decemberWaste: false, multiYear: false, WebApiOptions.PrnSearchDataType.AllDecemberWasteInFlashWindow);

        var content = await GetOk(SelectMultiplePath);

        CountCheckboxes(content).Should().Be(2);
        content.Should().NotContain("december-waste-flash-row");
        AcceptButtonIsVisible(content).Should().BeTrue();
    }

    [Test]
    public async Task SelectMultiplePrns_WhenDecemberWasteDisabled_AndMultiYearEnabled_SelectionFollowsThroughToConfirmation()
    {
        await SetUp(
            decemberWaste: false,
            multiYear: true,
            WebApiOptions.PrnSearchDataType.DecemberWasteInFlashWindow,
            WebApiOptions.PrnOrganisationDataType.DecemberWasteInFlashWindow);

        var selectPage = await GetOk(SelectMultiplePath);

        var selectableIds = Regex.Matches(selectPage, "chkBoxSelectPrn_[^_\"]*_(?<id>[0-9a-f-]{36})")
            .Select(m => m.Groups["id"].Value)
            .Distinct()
            .ToList();
        selectableIds.Should().BeEquivalentTo([StandardPrnId], "the year choice PRN is not offered for bulk selection");

        var form = new Dictionary<string, string>
        {
            { "Prns[0].ExternalId", selectableIds[0] },
            { "Prns[0].IsSelected", "true" },
            { "__RequestVerificationToken", "not-validated-in-component-tests" }
        };
        var passThrough = await Context.Client.PostAsync(AcceptMultiplePassThroughPath, form);
        passThrough.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var confirmPage = await GetOk(AcceptMultiplePath);

        confirmPage.Should().Contain("PRN-202");
        confirmPage.Should().NotContain("PRN-201");
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task SelectSinglePrn_ShowsFlashLabelOnlyWhenDecemberWasteEnabled(bool decemberWaste, bool expectFlash)
    {
        await SetUp(decemberWaste, multiYear: true);

        var content = await GetOk(SelectSinglePath);

        if (expectFlash)
            content.Should().Contain("Can be accepted towards 2026 or 2027");
        else
            content.Should().NotContain("Can be accepted towards");
    }

    [TestCase(Language.English)]
    [TestCase(Language.Welsh)]
    public async Task ChooseAcceptanceYear_WhenMultiYearEnabled_OffersBothYears(string language)
    {
        await SetUp(decemberWaste: true, multiYear: true);
        SetLanguage(language);

        var content = await GetOk(ChooseAcceptanceYearPath);

        content.Should().Contain("id=\"SelectedYear-2026\"");
        content.Should().Contain("id=\"SelectedYear-2027\"");
        await Verify(content, VerifyHtml.Extension, VerifyHtml.DefaultSettings)
            .ScrubCommonHtmlNodes()
            .UseParameters(language);
    }

    [Test]
    public async Task ChooseAcceptanceYear_WhenNoYearSelected_ShowsError()
    {
        await SetUp(decemberWaste: true, multiYear: true);

        var response = await Context.Client.PostAsync(ChooseAcceptanceYearPath, new Dictionary<string, string>());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("govuk-error-summary");
        content.Should().Contain("Select a year");
    }

    [Test]
    public async Task ChooseAcceptanceYear_WhenYearSelected_ConfirmPageShowsChosenYear()
    {
        await SetUp(decemberWaste: true, multiYear: true);

        var response = await Context.Client.PostAsync(ChooseAcceptanceYearPath, new Dictionary<string, string>
        {
            { "SelectedYear", "2027" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().EndWith(AcceptSinglePath);

        var content = await GetOk(AcceptSinglePath);
        content.Should().Contain("this PRN towards your 2027 recycling obligations?");
    }

    [Test]
    public async Task ChooseAcceptanceYear_WhenMultiYearDisabled_RedirectsToAcceptPrn()
    {
        await SetUp(decemberWaste: true, multiYear: false);

        var response = await Context.Client.GetAsync(ChooseAcceptanceYearPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().EndWith(AcceptSinglePath);
    }

    [Test]
    public async Task AcceptSinglePrn_WhenMultiYearEnabled_AndNoYearChosen_RedirectsToChooseAcceptanceYear()
    {
        await SetUp(decemberWaste: true, multiYear: true);

        var response = await Context.Client.GetAsync(AcceptSinglePath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().EndWith(ChooseAcceptanceYearPath);
    }

    [Test]
    public async Task AcceptSinglePrn_WhenMultiYearDisabled_ShowsConfirmPageForEarliestYear()
    {
        await SetUp(decemberWaste: true, multiYear: false);

        var content = await GetOk(AcceptSinglePath);

        content.Should().Contain("Accept this PRN towards your 2026 recycling obligations?");
    }

    [TestCase(true, new[] { "PRN-01" })]
    [TestCase(false, new[] { "PRN-01", "PRN-08" })]
    public async Task AcceptMultiplePrns_ExcludesPrnsWithYearChoiceOnlyWhenMultiYearEnabled(bool multiYear, string[] expectedPrns)
    {
        await SetUp(
            decemberWaste: false,
            multiYear,
            prnOrganisationData: WebApiOptions.PrnOrganisationDataType.DecemberWasteAwaitingAcceptance);
        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(nameof(FrontendSchemeRegistrationSession),
            Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
            {
                PrnSession = new PrnSession
                {
                    SelectedPrnIds =
                    [
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        new Guid("00000000-0000-0000-0000-000000000008")
                    ],
                    InitialNoteTypes = "PRNs"
                }
            })));

        var content = await GetOk(AcceptMultiplePath);

        foreach (var prn in new[] { "PRN-01", "PRN-08" })
        {
            if (expectedPrns.Contains(prn))
                content.Should().Contain(prn);
            else
                content.Should().NotContain(prn);
        }
    }

    private async Task SetUp(
        bool decemberWaste,
        bool multiYear,
        WebApiOptions.PrnSearchDataType prnSearchData = WebApiOptions.PrnSearchDataType.Default,
        WebApiOptions.PrnOrganisationDataType prnOrganisationData = WebApiOptions.PrnOrganisationDataType.Default)
    {
        Context.SetUp(
            overrideSession: true,
            additionalConfig: new Dictionary<string, string?>
            {
                { "FeatureManagement:ShowDecemberWaste", decemberWaste.ToString().ToLowerInvariant() },
                { "FeatureManagement:ShowMultiYearObligations", multiYear.ToString().ToLowerInvariant() },
                { "StartupUtcTimestampOverride", InFlashWindow }
            },
            webApiOptions: new WebApiOptions
            {
                PrnSearchData = prnSearchData,
                PrnOrganisationData = prnOrganisationData
            });

        await Context.Client.AuthenticateDefaultUser();
    }

    private void SetLanguage(string language) =>
        Context.GetSessionStore().Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(language));

    private async Task<string> GetOk(string path)
    {
        var response = await Context.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }

    private static bool AcceptButtonIsVisible(string content)
    {
        var button = Regex.Match(content, "<button id=\"acceptSelectedPrns\"[^>]*>");
        button.Success.Should().BeTrue("the accept button is always rendered when PRNs are listed");
        return !button.Value.Contains("hidden");
    }

    private static int CountCheckboxes(string content) =>
        content.Split("class=\"govuk-checkboxes__input\"").Length - 1;
}
