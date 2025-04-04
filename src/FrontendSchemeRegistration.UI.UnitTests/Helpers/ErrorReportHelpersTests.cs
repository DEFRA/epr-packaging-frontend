namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using Application.DTOs;
using FluentAssertions;
using TestHelpers;
using UI.Helpers;

[TestFixture]
public class ErrorReportHelpersTests
{
    [TestCase("en-GB", "01", "Organisation ID must be a 6 digit number - for example, 100123")]
    [TestCase("en-GB", "02", "When packaging activity is entered, it must be one of the codes SO, PF, IM, SE, HL or OM")]
    [TestCase("en-GB", "03", "Packaging type must be one of the codes HH, NH, CW, OW, PB, RU, HDC, NDC or SP")]
    [TestCase("en-GB", "04", "When packaging class is entered, it must be one of the codes P1, P2, P3, P4, P5, P6, O1, O2 or B1")]
    [TestCase("en-GB", "05", "Packaging material must be one of the codes AL, FC, GL, PC, PL, ST, WD or OT")]
    [TestCase("en-GB", "07", "When packaging type is self-managed waste (CW or OW), from country must be one of England (EN), Northern Ireland (NI), Scotland (SC) or Wales (WS)")]
    [TestCase("en-GB", "08", "When packaging type is self-managed waste (CW or OW), and you send it to another country, the to country must be one of England (EN), Northern Ireland (NI), Scotland (SC) or Wales (WS)")]
    [TestCase("en-GB", "09", "Packaging material weight must be a whole number in kilograms. For example, 50000. Do not include the words 'kilograms' or 'kgs'.")]
    [TestCase("en-GB", "10", "Packaging material units must be a whole number")]
    [TestCase("en-GB", "13", "Cannot be going from and to the same country in the UK")]
    [TestCase("en-GB", "14", "To country is not needed for this packaging type")]
    [TestCase("en-GB", "15", "From country is not needed for this packaging type")]
    [TestCase("en-GB", "22", "Invalid combination of organisation size and packaging activity and packaging type")]
    [TestCase("en-GB", "23", "For large organisations (L), when packaging activity is supplied through an online marketplace that you own (OM), packaging type must be household packaging (HH) or non-household packaging (NH)")]
    [TestCase("en-GB", "25", "When packaging activity is supplied through an online marketplace that you own (OM), and packaging type is household packaging (HH), packaging class must be online marketplace total (P6)")]
    [TestCase("en-GB", "26", "When packaging activity is supplied through an online marketplace that you own (OM), and packaging type is non-household packaging (NH), packaging class must be online marketplace total (P6)")]
    [TestCase("en-GB", "27", "Invalid combination of packaging activity and packaging type and packaging class")]
    [TestCase("en-GB", "28", "When packaging type is non-household waste (NH), packaging class must be one of the codes P1, P2, P3 or P4")]
    [TestCase("en-GB", "29", "Packaging type is self-managed consumer waste (CW) so packaging class can only be self-managed consumer waste - all (O1)")]
    [TestCase("en-GB", "30", "Packaging type is self-managed organisation waste (OW) so packaging class can only be organisation waste - origin (O2)")]
    [TestCase("en-GB", "31", "Packaging type is household packaging (HH) so packaging class can only be one of primary packaging (P1), shipment packaging (P3) or online marketplace total (P6)")]
    [TestCase("en-GB", "33", "Packaging type is commonly ends up in public bins (PB) so packaging class can only be public bin (B1)")]
    [TestCase("en-GB", "34", "Packaging type is household drinks containers (HDC) or non-household drinks containers (NDC) so do not enter a packaging class")]
    [TestCase("en-GB", "35", "Packaging type is reusable packaging (RU) so packaging class can only be primary packaging (P1) or non-primary reusable packaging (P5)")]
    [TestCase("en-GB", "36", "Invalid combination of packaging class and packaging type")]
    [TestCase("en-GB", "37", "Packaging material for household drinks containers (HDC) and non-household drinks containers (NDC) must be one of aluminium (AL), glass (GL), plastic (PL) or steel (ST)")]
    [TestCase("en-GB", "38", "Packaging material units not needed for this packaging type")]
    [TestCase("en-GB", "39", "Packaging type is household drinks containers (HDC) or non-household drinks containers (NDC) so packaging material quantity must be a whole number")]
    [TestCase("en-GB", "40", "Duplicate information submitted")]
    [TestCase("en-GB", "41", "Organisation size must be L. Currently, only large organisations can report packaging data.")]
    [TestCase("en-GB", "42", "When packaging activity is entered, packaging type cannot be self-managed waste (CW and OW)")]
    [TestCase("en-GB", "43", "When packaging activity is not entered, packaging type can only be self-managed waste (CW and OW)")]
    [TestCase("en-GB", "44", "Enter the time period for submission")]
    [TestCase("en-GB", "45", "When packaging material is other (OT), you must enter the name of the material in packaging material subtype. It must not include numbers or commas.")]
    [TestCase("en-GB", "46", "Subsidiary ID must only include letters a to z, and numbers. It must be 32 characters or less.")]
    [TestCase("en-GB", "47", "Packaging material subtype not needed for this packaging material")]
    [TestCase("en-GB", "48", "Total weight of a single packaging material transferred to any country must not be more than the total collected. For example, you cannot transfer more plastic than you collect. Check the weight of this packaging material transferred to another country for all rows for this organisation.")]
    [TestCase("en-GB", "49", "When packaging type is self-managed waste (CW and OW), you must enter the from country")]
    [TestCase("en-GB", "50", "Submission period must be the same for all of this organisation's packaging data")]
    [TestCase("en-GB", "51", "Packaging material subtype cannot be plastic, HDPE or PET")]
    [TestCase("en-GB", "53", "When organisation size is large (L), packaging type cannot be small organisation packaging - all (SP)")]
    [TestCase("en-GB", "54", "You're reporting packaging data for January to June 2023. Submission period must be 2023-P1 or 2023-P2.")]
    [TestCase("en-GB", "55", "You're reporting packaging data for July to December 2023. Submission period must be 2023-P3.")]
    [TestCase("en-GB", "57", "Organisation ID must be the same as the identification number assigned to this organisation when they created their account. Check the organisation ID for all rows for this organisation.")]
    [TestCase("en-GB", "58", "Organisation ID is not linked to your compliance scheme. Check the organisation ID for all rows for this organisation.")]
    [TestCase("en-GB", "59", "Packaging material weight is less than 100. Check all packaging weights are in kg and not tonnes.")]
    [TestCase("en-GB", "60", "Check the packaging material weight for all rows for this organisation. If the total for the reporting year is between 25,000kg and 50,000kg, this organisation may only need to register as a small organisation in the following year.")]
    [TestCase("en-GB", "62", "Only one packaging material reported for this organisation. Check you have entered all packaging materials. All must be reported separately.")]
    [TestCase("en-GB", "63", "For self-managed consumer waste (CW), you may not be able to offset your own packaging against aluminium (AL), glass (GL), paper or card (PC), or steel (ST). To check what packaging materials you can offset, you'll need to contact your nation's environmental regulator.")]    
    [TestCase("en-GB", "ErrorIssue", "Error")]
    [TestCase("en-GB", "WarningIssue", "Warning")]

    // Welsh
    [TestCase("cy-GB", "60", "Gwiriwch bwysau'r deunydd pecynwaith ar gyfer pob rhes i'r sefydliad yma. Os yw cyfanswm y flwyddyn adrodd rhwng 25,000kg a 50,000kg, efallai mai dim ond fel sefydliad bach y bydd angen i'r sefydliad yma gofrestru yn y flwyddyn ganlynol.")]
    [TestCase("cy-GB", "63", "Yn achos gwastraff defnyddwyr hunan-reoledig (CW), mae'n bosibl na fyddwch chi'n gallu gwrthbwyso'ch pecynwaith eich hunan yn erbyn alwminiwm (AL), gwydr (GL), papur neu gerdyn (PC), neu ddur (ST). I wirio pa ddeunyddiau pecynwaith y gallwch eu gwrthbwyso, bydd angen ichi gysylltu â rheoleiddiwr amgylcheddol eich gwlad.")]    

    public void ToErrorReportRows_ConvertsValidationErrorsToErrorRowsWithMessage_WhenCalled(
        string culture, string errorCode, string expectedMessage)
    {
        // Arrange
        CultureHelpers.SetCulture(culture);

        var issueType = string.Empty;

        if (culture == "en-GB")
        {
            issueType =
                errorCode == "ErrorIssue" ? "Error" : "Warning";
        }
        else
        {
           issueType =
                errorCode == "ErrorIssue" ? "Gwall" : null;
        }

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
   
    [Test]
    public void ToRegistrationErrorReportRows_ConvertsRegistrationValidationErrorsToRegistrationErrorRowsWithMessageAndIndex_WhenCalled()
    {
        // Arrange
        CultureHelpers.SetCulture("en-GB");

        const string organisationId = "123456";
        const string subsidiaryId = "123456";
        var validationIssues = new List<RegistrationValidationError>
        {
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Error",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "801",
                        ColumnIndex = 0,
                        ColumnName = "testColumnName"
                    }
                },
            },
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Error",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "802",
                        ColumnIndex = 10,
                        ColumnName = "testColumnName2"
                    }
                },
            },
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Error",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "803",
                        ColumnIndex = 30,
                        ColumnName = "testColumnName3"
                    }
                },
            },
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Error",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "804",
                        ColumnIndex = 55,
                        ColumnName = "testColumnName4"
                    }
                },
            },
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Warning",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "73",
                        ColumnIndex = 17,
                        ColumnName = "testColumnName5"
                    }
                },
            },
        };

        // Act
        var result = validationIssues.ToRegistrationErrorReportRows();

        // Assert
        var expected = new List<RegistrationErrorReportRow>
        {
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "A",
                ColumnName = "testColumnName",
                IssueType = "Error",
                Message = "Enter the organisation ID"
            },
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "K",
                ColumnName = "testColumnName2",
                IssueType = "Error",
                Message = "Enter the organisation name"
            },
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "AE",
                ColumnName = "testColumnName3",
                IssueType = "Error",
                Message = "Enter the home nation code"
            },
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "BD",
                ColumnName = "testColumnName4",
                IssueType = "Error",
                Message = "Home nation code must be one of EN NI SC or WS"
            },
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "R",
                ColumnName = "testColumnName5",
                IssueType = "Warning",
                Message = "Turnover for this organization is recorded as zero. Please verify this is correct before submitting a zero return"
            },
        };

        result.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ToRegistrationErrorReportRows_ConvertsRegistrationValidation_ErrorsAndWarnings_RowsWith_CorrectMessage_WhenCalledFor_WelshLanguage()
    {
        // Arrange
        CultureHelpers.SetCulture("cy-GB");

        const string organisationId = "123456";
        const string subsidiaryId = "123456";
        var validationIssues = new List<RegistrationValidationError>
        {
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Error",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "894",
                        ColumnIndex = 0,
                        ColumnName = "testColumnName"
                    }
                },
            },
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Error",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "896",
                        ColumnIndex = 10,
                        ColumnName = "testColumnName2"
                    }
                },
            },
            new ()
            {
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                RowNumber = 1,
                IssueType = "Warning",
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "73",
                        ColumnIndex = 30,
                        ColumnName = "testColumnName3"
                    }
                },
            },
        };

        // Act
        var result = validationIssues.ToRegistrationErrorReportRows();

        // Assert
        var expected = new List<RegistrationErrorReportRow>
        {
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "A",
                ColumnName = "testColumnName",
                IssueType = "Gwall",
                Message = "Dim ond bach neu fawr y gall maint y sefydliad fod."
            },
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "K",
                ColumnName = "testColumnName2",
                IssueType = "Gwall",
                Message = "I fod yn gymwys fel cynhyrchwr bach, rhaid i'ch trosiant blynyddol diweddaraf fod yn £2 filiwn neu lai, neu rhaid i gyfanswm eich tunelli blynyddol fod yn 50 tunnell neu lai."
            },
            new ()
            {
                Row = "1",
                OrganisationId = organisationId,
                SubsidiaryId = subsidiaryId,
                Column = "AE",
                ColumnName = "testColumnName3",
                IssueType = "Rhybudd",
                Message = "Mae trosiant y sefydliad yma wedi'i gofnodi fel sero. Gwiriwch fod hyn yn gywir cyn ichi gyflwyno ffurflen sy'n dweud sero"
            }
        };

        result.Should().BeEquivalentTo(expected);
    }
}