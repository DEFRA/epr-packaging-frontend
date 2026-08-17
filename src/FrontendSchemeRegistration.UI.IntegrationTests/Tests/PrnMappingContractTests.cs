namespace FrontendSchemeRegistration.UI.IntegrationTests.Tests;

using System.Net;
using FluentAssertions;

[TestFixture]
public class PrnMappingContractTests : TestBase
{
    private const string FullyPopulatedPrnId = "00000000-0000-0000-0000-000000000007";
    private const string PernId              = "00000000-0000-0000-0000-000000000008";
    private const string CanceledPrnId       = "00000000-0000-0000-0000-000000000009";

    private static object FullyPopulatedPrn => new
    {
        id = 7,
        externalId = FullyPopulatedPrnId,
        prnNumber = "CONTRACT-PRN-007",
        materialName = "Aluminium",
        issueDate = "2025-06-15T10:30:00",
        prnStatus = "AWAITINGACCEPTANCE",
        tonnageValue = 999,
        obligationYear = "2025",
        issuedByOrg = "Acme Reprocessors Ltd",
        issuerNotes = "Important note about this PRN",
        prnSignatory = "Jane Smith",
        prnSignatoryPosition = "Director",
        reprocessingSite = "42 Factory Road, Manchester",
        organisationName = "Test Producer Ltd",
        isExport = false,
        decemberWaste = false
    };

    private static object PernPrn => new
    {
        id = 8,
        externalId = PernId,
        prnNumber = "CONTRACT-PERN-008",
        materialName = "Glass remelt",
        issueDate = "2025-06-15T10:30:00",
        prnStatus = "AWAITINGACCEPTANCE",
        tonnageValue = 500,
        obligationYear = "2025",
        isExport = true
    };

    private static object CanceledPrn => new
    {
        id = 9,
        externalId = CanceledPrnId,
        prnNumber = "CONTRACT-PRN-009",
        materialName = "Fibre",
        issueDate = "2025-06-15T10:30:00",
        prnStatus = "CANCELED",
        tonnageValue = 1,
        obligationYear = "2025",
        isExport = false,
        prnSignatoryPosition = (string?)null,
        processToBeUsed = (string?)null
    };

    private const string DecemberWastePrnId = "00000000-0000-0000-0000-000000000010";

    private static object DecemberWastePrn => new
    {
        id = 10,
        externalId = DecemberWastePrnId,
        prnNumber = "CONTRACT-PRN-010",
        materialName = "Aluminium",
        issueDate = "2025-06-15T10:30:00",
        prnStatus = "AWAITINGACCEPTANCE",
        tonnageValue = 999,
        obligationYear = "2025",
        isExport = false,
        decemberWaste = true
    };

    // prnNumber → PrnOrPernNumber
    // issuedByOrg → IssuedBy
    // prnSignatory → AuthorisedBy
    // prnSignatoryPosition → Position
    // reprocessingSite → ReproccessingSiteAddress
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithAllRenamedFields_WhenAllSourceFieldsArePopulated()
    {
        SetupPrnById(FullyPopulatedPrnId, FullyPopulatedPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("CONTRACT-PRN-007",           because: "prnNumber should map to PrnOrPernNumber");
        content.Should().Contain("Acme Reprocessors Ltd",      because: "issuedByOrg should map to IssuedBy");
        content.Should().Contain("Jane Smith",                  because: "prnSignatory should map to AuthorisedBy");
        content.Should().Contain("Director",                    because: "prnSignatoryPosition should map to Position");
        content.Should().Contain("42 Factory Road, Manchester", because: "reprocessingSite should map to ReproccessingSiteAddress");
    }

    // obligationYear string "2025" → int 2025 → rendered as year on page
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithObligationYear_WhenObligationYearIsAValidString()
    {
        SetupPrnById(FullyPopulatedPrnId, FullyPopulatedPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("2025", because: "obligationYear '2025' should parse to int and render as the year of issue");
    }

    // prnStatus "AWAITINGACCEPTANCE" → PrnStatus.AwaitingAcceptance ("AWAITING ACCEPTANCE")
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithTranslatedStatus_WhenStatusIsAwaitingAcceptance()
    {
        SetupPrnById(FullyPopulatedPrnId, FullyPopulatedPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("AWAITING ACCEPTANCE",
            because: "AWAITINGACCEPTANCE (API value) should be translated to AWAITING ACCEPTANCE (display value)");
    }

    // prnStatus "CANCELED" → PrnStatus.Cancelled ("CANCELLED")
    // Note: the source API sends "CANCELED" (one L); the mapped display value is "CANCELLED" (two L).
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithTranslatedStatus_WhenStatusIsCanceled()
    {
        SetupPrnById(CanceledPrnId, CanceledPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{CanceledPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CANCELLED",
            because: "CANCELED (API value, one L) should be translated to CANCELLED (display value, two L)");
        content.Should().NotContain("CANCELED",
            because: "the raw un-translated API value should not leak into the UI");
    }

    // isExport false → NoteType "PRN" → IsPrn=true → "Packaging Waste Recycling Note" heading and "PRN number:" label
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithPrnHeadingAndLabel_WhenIsExportIsFalse()
    {
        SetupPrnById(FullyPopulatedPrnId, FullyPopulatedPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Packaging Waste Recycling Note",
            because: "isExport=false should map NoteType to PRN, rendering the PRN-specific heading");
        content.Should().Contain("PRN number:",
            because: "isExport=false should map NoteType to PRN, rendering the PRN number label");
    }

    // isExport true → NoteType "PERN" → IsPrn=false → "PERN number:" label
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithPernLabel_WhenIsExportIsTrue()
    {
        SetupPrnById(PernId, PernPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{PernId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CONTRACT-PERN-008",
            because: "prnNumber should appear as PrnOrPernNumber on the PERN detail page");
        content.Should().Contain("PERN number:",
            because: "isExport=true should map NoteType to PERN, rendering the PERN number label");
        content.Should().NotContain("Packaging Waste Recycling Note",
            because: "the PRN-specific heading should not appear for a PERN");
    }

    // null PrnSignatoryPosition → Position maps to "" not null → page renders without exception
    // null ProcessToBeUsed → RecyclingProcess maps to "" not null → page renders without exception
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithoutException_WhenOptionalFieldsAreNull()
    {
        SetupPrnById(CanceledPrnId, CanceledPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{CanceledPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK,
            because: "null PrnSignatoryPosition and ProcessToBeUsed should map to empty string, not throw");
    }

    // decemberWaste true → IsDecemberWaste=true → December waste warning rendered with the obligation year
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithDecemberWasteWarning_WhenDecemberWasteIsTrue()
    {
        SetupPrnById(DecemberWastePrnId, DecemberWastePrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{DecemberWastePrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("This PRN relates to waste received for reprocessing in December 2025",
            because: "decemberWaste=true should map to IsDecemberWaste and render the December waste warning for the obligation year");
    }

    // decemberWaste false → IsDecemberWaste=false → no December waste warning rendered
    [Test]
    [Category("IntegrationTest")]
    public async Task SelectedPrn_ReturnsOk_WithoutDecemberWasteWarning_WhenDecemberWasteIsFalse()
    {
        SetupPrnById(FullyPopulatedPrnId, FullyPopulatedPrn);

        var response = await Client.GetAsync($"/report-data/selected-prn/{FullyPopulatedPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("relates to waste received for reprocessing in December",
            because: "decemberWaste=false should map to IsDecemberWaste=false and not render the warning");
    }

    // CSV column headers: all renamed fields should appear under their mapped names
    [Test]
    [Category("IntegrationTest")]
    public async Task CsvDownload_ReturnsOk_WithCorrectColumnHeaders()
    {
        SetupPrnOrganisationList([FullyPopulatedPrn]);

        var response = await Client.GetAsync("/report-data/download-prns-csv");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var headerRow = content.Split('\n')[0].TrimEnd('\r');

        headerRow.Should().Be(
            "PRN or PERN number,PRN or PERN,Status,Issued by,Issued to,Accreditation number,Date issued,December waste,Material,Recycling process,Tonnes,Date accepted,Date cancelled,Issuer note",
            because: "all renamed fields should appear under their mapped display names in the CSV header");
    }

    // AcceptSinglePrn heading text switches to the "are you sure" confirm wording
    // when FeatureManagement:ShowMultiYearObligations is enabled.
    [Test]
    [Category("IntegrationTest")]
    public async Task AcceptSinglePrn_ReturnsOk_WithConfirmHeading_WhenMultiYearObligationsEnabled()
    {
        await ReconfigureAsync(new Dictionary<string, string?>
        {
            ["FeatureManagement:ShowMultiYearObligations"] = "true"
        });
        SetupPrnById(FullyPopulatedPrnId, FullyPopulatedPrn);

        var response = await Client.GetAsync($"/report-data/accept-prn/{FullyPopulatedPrnId}");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Are you sure you want to accept this PRN",
            because: "ShowMultiYearObligations=true should render the confirm heading instead of the plain accept heading");
    }

    // CSV data row: verify field renames appear in the correct column positions
    //   prnNumber    → column 1  (PRN or PERN number)
    //   isExport     → column 2  (PRN or PERN: "PRN" when false, "PERN" when true)
    //   tonnageValue → column 11 (Tonnes)
    [Test]
    [Category("IntegrationTest")]
    public async Task CsvDownload_ReturnsOk_WithFieldsMappedToCorrectColumnPositions()
    {
        SetupPrnOrganisationList([FullyPopulatedPrn]);

        var response = await Client.GetAsync("/report-data/download-prns-csv");

        response.Should().HaveStatusCode(HttpStatusCode.OK);
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
