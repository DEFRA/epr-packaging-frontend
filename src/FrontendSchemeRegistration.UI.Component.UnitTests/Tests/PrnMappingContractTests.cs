namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using Extensions;
using FluentAssertions;
using Infrastructure;
using NUnit.Framework;

/// <summary>
/// Contract tests for PrnModelMapper — verify that fields are renamed and values translated
/// correctly from the backend API response through to the rendered HTML and CSV output.
/// These tests guard against regressions when the mapping implementation changes.
/// </summary>
public class PrnMappingContractTests
{
    private const string FullyPopulatedPrnId = "00000000-0000-0000-0000-000000000007";
    private const string PernId              = "00000000-0000-0000-0000-000000000008";
    private const string CancelledPrnId      = "00000000-0000-0000-0000-000000000009";

    private ComponentTestContext Context { get; } = new();

    [SetUp]
    public async Task SetUp()
    {
        Context.SetUp();
        await Context.Client.AuthenticateDefaultUser();
    }

    [TearDown]
    public void TearDown() => Context.Dispose();

    // prnNumber → PrnOrPernNumber
    // issuedByOrg → IssuedBy
    // prnSignatory → AuthorisedBy
    // prnSignatoryPosition → Position
    // reprocessingSite → ReproccessingSiteAddress
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithAllRenamedFields_WhenAllSourceFieldsArePopulated()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("CONTRACT-PRN-007",           because: "prnNumber should map to PrnOrPernNumber");
        content.Should().Contain("Acme Reprocessors Ltd",      because: "issuedByOrg should map to IssuedBy");
        content.Should().Contain("Jane Smith",                  because: "prnSignatory should map to AuthorisedBy");
        content.Should().Contain("Director",                    because: "prnSignatoryPosition should map to Position");
        content.Should().Contain("42 Factory Road, Manchester", because: "reprocessingSite should map to ReproccessingSiteAddress");
    }

    // obligationYear string "2025" → int 2025 → rendered as year on page
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithObligationYear_WhenObligationYearIsAValidString()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("2025", because: "obligationYear '2025' should parse to int and render as the year of issue");
    }

    // prnStatus "AWAITINGACCEPTANCE" → PrnStatus.AwaitingAcceptance ("AWAITING ACCEPTANCE")
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithTranslatedStatus_WhenStatusIsAwaitingAcceptance()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("AWAITING ACCEPTANCE",
            because: "AWAITINGACCEPTANCE (API value) should be translated to AWAITING ACCEPTANCE (display value)");
    }

    // prnStatus "CANCELED" (one L) → PrnStatus.Cancelled → "CANCELLED" (two L)
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithTranslatedStatus_WhenStatusIsCanceled()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{CancelledPrnId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CANCELLED",
            because: "CANCELED (API value, one L) should be translated to CANCELLED (display value, two L)");
        content.Should().NotContain("CANCELED",
            because: "the raw un-translated API value should not leak into the UI");
    }

    // isExport false → NoteType "PRN" → "Packaging Waste Recycling Note" heading and "PRN number:" label
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithPrnHeadingAndLabel_WhenIsExportIsFalse()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Packaging Waste Recycling Note",
            because: "isExport=false should map NoteType to PRN, rendering the PRN-specific heading");
        content.Should().Contain("PRN number:",
            because: "isExport=false should map NoteType to PRN, rendering the PRN number label");
    }

    // isExport true → NoteType "PERN" → "PERN number:" label, no PRN heading
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithPernLabel_WhenIsExportIsTrue()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{PernId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CONTRACT-PERN-008",
            because: "prnNumber should appear as PrnOrPernNumber on the PERN detail page");
        content.Should().Contain("PERN number:",
            because: "isExport=true should map NoteType to PERN, rendering the PERN number label");
        content.Should().NotContain("Packaging Waste Recycling Note",
            because: "the PRN-specific heading should not appear for a PERN");
    }

    // null PrnSignatoryPosition and ProcessToBeUsed should map to empty string, not throw
    [Test]
    public async Task SelectedPrn_ReturnsOk_WithoutException_WhenOptionalFieldsAreNull()
    {
        var response = await Context.Client.GetAsync($"/report-data/selected-prn/{CancelledPrnId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "null PrnSignatoryPosition and ProcessToBeUsed should map to empty string, not throw");
    }

    // CSV column headers: all renamed fields should appear under their mapped names
    [Test]
    public async Task CsvDownload_ReturnsOk_WithCorrectColumnHeaders()
    {
        var response = await Context.Client.GetAsync("/report-data/download-prns-csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var headerRow = content.Split('\n')[0].TrimEnd('\r');

        headerRow.Should().Be(
            "PRN or PERN number,PRN or PERN,Status,Issued by,Issued to,Accreditation number,Date issued,December waste,Material,Recycling process,Tonnes,Date accepted,Date cancelled,Issuer note",
            because: "all renamed fields should appear under their mapped display names in the CSV header");
    }

    // CSV data row: prnNumber → col 1, isExport → col 2, tonnageValue → col 11
    [Test]
    public async Task CsvDownload_ReturnsOk_WithFieldsMappedToCorrectColumnPositions()
    {
        var response = await Context.Client.GetAsync("/report-data/download-prns-csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var dataRow = content.Split('\n')
            .Select(r => r.TrimEnd('\r'))
            .FirstOrDefault(r => r.StartsWith("CONTRACT-PRN-007"));

        dataRow.Should().NotBeNull(because: "CONTRACT-PRN-007 should appear in the CSV response");

        var columns = dataRow!.Split(',');
        columns[0].Should().Be("CONTRACT-PRN-007", because: "prnNumber maps to PrnOrPernNumber in column 1");
        columns[1].Should().Be("PRN",              because: "isExport=false maps to NoteType 'PRN' in column 2");
        columns[10].Should().Be("999",             because: "tonnageValue=999 maps to Tonnes in column 11");
    }
}
