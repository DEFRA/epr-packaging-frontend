namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using Application.DTOs;
using FluentAssertions;
using TestHelpers;
using UI.Helpers;

[TestFixture]
public class ErrorReportHelpersTests
{
    [TestCase("01", "Organisation ID must be a 6 digit number - for example, 100123")]
    [TestCase("02", "When packaging activity is entered, it must be one of the codes SO, PF, IM, SE, HL or OM")]
    [TestCase("03", "Packaging type must be one of the codes HH, NH, CW, OW, PB, RU, HDC, NDC or SP")]
    [TestCase("04", "When packaging class is entered, it must be one of the codes P1, P2, P3, P4, P5, P6, O1, O2 or B1")]
    [TestCase("05", "Packaging material must be one of the codes AL, FC, GL, PC, PL, ST, WD or OT")]
    [TestCase("07", "When packaging type is self-managed waste (CW or OW), from country must be one of England (EN), Northern Ireland (NI), Scotland (SC) or Wales (WS)")]
    [TestCase("08", "When packaging type is self-managed waste (CW or OW), and you send it to another country, the to country must be one of England (EN), Northern Ireland (NI), Scotland (SC) or Wales (WS)")]
    [TestCase("09", "Packaging material weight must be a whole number in kilograms. For example, 50000. Do not include the words 'kilograms' or 'kgs'.")]
    [TestCase("10", "Packaging material units must be a whole number")]
    [TestCase("13", "Cannot be going from and to the same country in the UK")]
    [TestCase("14", "To country is not needed for this packaging type")]
    [TestCase("15", "From country is not needed for this packaging type")]
    [TestCase("22", "Invalid combination of organisation size and packaging activity and packaging type")]
    [TestCase("23", "For large organisations (L), when packaging activity is supplied through an online marketplace that you own (OM), packaging type must be household packaging (HH) or non-household packaging (NH)")]
    [TestCase("25", "When packaging activity is supplied through an online marketplace that you own (OM), and packaging type is household packaging (HH), packaging class must be online marketplace total (P6)")]
    [TestCase("26", "When packaging activity is supplied through an online marketplace that you own (OM), and packaging type is non-household packaging (NH), packaging class must be online marketplace total (P6)")]
    [TestCase("27", "Invalid combination of packaging activity and packaging type and packaging class")]
    [TestCase("28", "When packaging type is non-household waste (NH), packaging class must be one of the codes P1, P2, P3 or P4")]
    [TestCase("29", "Packaging type is self-managed consumer waste (CW) so packaging class can only be self-managed consumer waste - all (O1)")]
    [TestCase("30", "Packaging type is self-managed organisation waste (OW) so packaging class can only be organisation waste - origin (O2)")]
    [TestCase("31", "Packaging type is household packaging (HH) so packaging class can only be one of primary packaging (P1), shipment packaging (P3) or online marketplace total (P6)")]
    [TestCase("33", "Packaging type is commonly ends up in public bins (PB) so packaging class can only be public bin (B1)")]
    [TestCase("34", "Packaging type is household drinks containers (HDC) or non-household drinks containers (NDC) so do not enter a packaging class")]
    [TestCase("35", "Packaging type is reusable packaging (RU) so packaging class can only be primary packaging (P1) or non-primary reusable packaging (P5)")]
    [TestCase("36", "Invalid combination of packaging class and packaging type")]
    [TestCase("37", "Packaging material for household drinks containers (HDC) and non-household drinks containers (NDC) must be one of aluminium (AL), glass (GL), plastic (PL) or steel (ST)")]
    [TestCase("38", "Packaging material units not needed for this packaging type")]
    [TestCase("39", "Packaging type is household drinks containers (HDC) or non-household drinks containers (NDC) so packaging material quantity must be a whole number")]
    [TestCase("40", "Duplicate information submitted")]
    [TestCase("41", "Organisation size must be L. Currently, only large organisations can report packaging data.")]
    [TestCase("42", "When packaging activity is entered, packaging type cannot be self-managed waste (CW and OW)")]
    [TestCase("43", "When packaging activity is not entered, packaging type can only be self-managed waste (CW and OW)")]
    [TestCase("44", "Enter the time period for submission")]
    [TestCase("45", "When packaging material is other (OT), you must enter the name of the material in packaging material subtype. It must not include numbers or commas.")]
    [TestCase("46", "Subsidiary ID must only include letters a to z, and numbers. It must be 32 characters or less.")]
    [TestCase("47", "Packaging material subtype not needed for this packaging material")]
    [TestCase("48", "Total weight of a single packaging material transferred to any country must not be more than the total collected. For example, you cannot transfer more plastic than you collect. Check the weight of this packaging material transferred to another country for all rows for this organisation.")]
    [TestCase("49", "When packaging type is self-managed waste (CW and OW), you must enter the from country")]
    [TestCase("50", "Submission period must be the same for all of this organisation's packaging data")]
    [TestCase("51", "Packaging material subtype cannot be plastic, HDPE or PET")]
    [TestCase("53", "When organisation size is large (L), packaging type cannot be small organisation packaging - all (SP)")]
    [TestCase("54", "You're reporting packaging data for January to June 2023. Submission period must be 2023-P1 or 2023-P2.")]
    [TestCase("55", "You're reporting packaging data for July to December 2023. Submission period must be 2023-P3.")]
    [TestCase("58", "Organisation ID is not linked to your compliance scheme. Check the organisation ID for all rows for this organisation.")]
    [TestCase("59", "Packaging material weight is less than 100. Check all packaging weights are in kg and not tonnes.")]
    [TestCase("62", "Only one packaging material reported for this organisation. Check you have entered all packaging materials. All must be reported separately.")]
    [TestCase("ErrorIssue", "Error")]
    [TestCase("WarningIssue", "Warning")]
    public void ToErrorReportRows_ConvertsValidationErrorsToErrorRowsWithMessage_WhenCalled(string errorCode, string expectedMessage)
    {
        // Arrange
        CultureHelpers.SetCulture("en-GB");

        var issueType = errorCode == "ErrorIssue" ? "Error" : "Warning";

        const string producerId = "123456";
        const string subsidiaryId = "123456";
        var validationErrors = new List<ProducerValidationError>
        {
            new ()
            {
                ProducerId = producerId,
                SubsidiaryId = subsidiaryId,
                ProducerType = "OL",
                DataSubmissionPeriod = "2023-P1",
                ProducerSize = "L",
                WasteType = "WT",
                PackagingCategory = "PC",
                MaterialType = "MT",
                MaterialSubType = "MST",
                FromHomeNation = "FHN",
                ToHomeNation = "THN",
                QuantityKg = "1",
                QuantityUnits = "1",
                RowNumber = 1,
                Issue = issueType,
                ErrorCodes = new List<string>
                {
                    errorCode,
                },
            },
        };

        // Act
        var result = validationErrors.ToErrorReportRows();

        // Assert
        var expected = new List<ErrorReportRow>
        {
            new ()
            {
                ProducerId = producerId,
                SubsidiaryId = subsidiaryId,
                ProducerType = "OL",
                DataSubmissionPeriod = "2023-P1",
                ProducerSize = "L",
                WasteType = "WT",
                PackagingCategory = "PC",
                MaterialType = "MT",
                MaterialSubType = "MST",
                FromHomeNation = "FHN",
                ToHomeNation = "THN",
                QuantityKg = "1",
                QuantityUnits = "1",
                RowNumber = 1,
                Issue = issueType,
                Message = expectedMessage,
            },
        };

        result.Should().BeEquivalentTo(expected);
    }
}