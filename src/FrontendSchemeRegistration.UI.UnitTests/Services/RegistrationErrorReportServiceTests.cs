namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using System.Text;
using Application.DTOs;
using Application.Services.Interfaces;
using FluentAssertions;
using Moq;
using TestHelpers;
using UI.Services;

[TestFixture]
public class RegistrationErrorReportServiceTests
{
    private static readonly Guid SubmissionId = Guid.NewGuid();
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private ErrorReportService _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _systemUnderTest = new ErrorReportService(_webApiGatewayClientMock.Object);

        _webApiGatewayClientMock.Setup(x => x.GetRegistrationValidationErrorsAsync(SubmissionId)).ReturnsAsync(new List<RegistrationValidationError>
        {
            new ()
            {
                OrganisationId = "def456",
                SubsidiaryId = "abc123",
                RowNumber = 1,
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "801",
                        ColumnIndex = 0,
                        ColumnName = "organisation_id"
                    }
                },
                IssueType = "Error"
            },
            new ()
            {
                OrganisationId = "test456",
                SubsidiaryId = "test123",
                RowNumber = 1,
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "61",
                        ColumnIndex = 0,
                        ColumnName = "organisation_id"
                    }
                },
                IssueType = "Warning"
            }
        });
    }

    [Test]
    public async Task GetRegistrationErrorReportStreamAsync_ReturnsErrorReportStream_WhenCalled()
    {
        // Arrange
        CultureHelpers.SetCulture("en-GB");

        var expectedHeaderRow = new[]
        {
            "Row",
            "Org ID",
            "Subsidiary ID",
            "Column",
            "Column name",
            "Issue",
            "Message",
        };
        var expectedFirstRow = new[]
        {
            "1",
            "def456",
            "abc123",
            "A",
            "organisation_id",
            "Error",
            "Enter the organisation ID"
        };

        // Act
        var result = await _systemUnderTest.GetRegistrationErrorReportStreamAsync(SubmissionId);

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