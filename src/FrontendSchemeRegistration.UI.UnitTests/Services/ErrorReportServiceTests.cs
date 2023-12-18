namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using System.Text;
using Application.DTOs;
using Application.Services.Interfaces;
using FluentAssertions;
using Moq;
using TestHelpers;
using UI.Services;
using UI.Services.Interfaces;

[TestFixture]
public class ErrorReportServiceTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private IErrorReportService _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _systemUnderTest = new ErrorReportService(_webApiGatewayClientMock.Object);

        _webApiGatewayClientMock.Setup(x => x.GetProducerValidationErrorsAsync(SubmissionId)).ReturnsAsync(new List<ProducerValidationError>
        {
            new ()
            {
                ProducerId = "123456",
                SubsidiaryId = "abc123",
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
                Issue = "Error",
                ErrorCodes = new List<string>
                {
                    "01",
                },
            },
        });
    }

    [Test]
    public async Task GetErrorReportStreamAsync_ReturnsErrorReportStream_WhenCalled()
    {
        // Arrange
        CultureHelpers.SetCulture("en-GB");

        var expectedHeaderRow = new[]
        {
            "organisation_id",
            "subsidiary_id",
            "organisation_size",
            "submission_period",
            "packaging_activity",
            "packaging_type",
            "packaging_class",
            "packaging_material",
            "packaging_material_subtype",
            "from_country",
            "to_country",
            "packaging_material_weight",
            "packaging_material_units",
            "Row Number",
            "Message",
            "Issue"
        };
        var expectedFirstRow = new[]
        {
            "123456",
            "abc123",
            "L",
            "2023-P1",
            "OL",
            "WT",
            "PC",
            "MT",
            "MST",
            "FHN",
            "THN",
            "1",
            "1",
            "1",
            "Organisation ID must be a 6 digit number - for example, 100123",
            "Error"
        };

        // Act
        var result = await _systemUnderTest.GetErrorReportStreamAsync(SubmissionId);

        // Assert
        var streamReader = new StreamReader(result);

        var actualHeaderRow = await streamReader.ReadLineAsync();
        var actualFirstLine = await streamReader.ReadLineAsync();

        actualHeaderRow.Split(",").Should().BeEquivalentTo(expectedHeaderRow);
        CustomCsvSplit(actualFirstLine).Should().BeEquivalentTo(expectedFirstRow);
    }

    private static string[] CustomCsvSplit(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == ',' && !inQuotes)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());
        return result.ToArray();
    }
}