using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.Extensions;

namespace FrontendSchemeRegistration.Application.UnitTests.Extensions;

[TestFixture]
public class HttpClientExtensionsTests
{
    private HttpClient _httpClient;

    [SetUp]
    public void SetUp()
    {
        _httpClient = new();
    }

    [Test]
    public void AddHeaderSubmissionPeriod_AddsSubmissionPeriodHeader()
    {
        // Arrange
        const string submissionPeriod = "Jan to Jun 23";

        // Act
        _httpClient.AddHeaderSubmissionPeriod(submissionPeriod);

        // Assert
        _httpClient.DefaultRequestHeaders.Should()
            .HaveCount(1)
            .And
            .ContainKey("SubmissionPeriod")
            .WhoseValue
            .Should()
            .BeEquivalentTo(submissionPeriod);
    }

    [Test]
    public void AddRegistrationSetId_AddsRegistrationSetIdHeader()
    {
        // Arrange
        var regSetId = Guid.NewGuid();

        // Act
        _httpClient.AddHeaderRegistrationSetIdIfNotNull(regSetId);

        // Assert
        _httpClient.DefaultRequestHeaders.Should()
            .HaveCount(1)
            .And
            .ContainKey("RegistrationSetId")
            .WhoseValue
            .Should()
            .BeEquivalentTo(regSetId.ToString());
    }

    [Test]
    public void AddHeaderComplianceSchemeIdIfNotNull_AddsComplianceSchemeIdHeader()
    {
        // Arrange
        var complianceSchemeId = Guid.NewGuid();

        // Act
        _httpClient.AddHeaderComplianceSchemeIdIfNotNull(complianceSchemeId);

        // Assert
        _httpClient.DefaultRequestHeaders.Should()
            .HaveCount(1)
            .And
            .ContainKey(ComplianceScheme.ComplianceSchemeId)
            .WhoseValue
            .Should()
            .BeEquivalentTo(complianceSchemeId.ToString());
    }
    
    [Test]
    public void AddHeaderIsResubmissionIfNotNull_AddsComplianceSchemeIdHeader()
    {
        // Arrange
        // Act
        _httpClient.AddHeaderIsResubmissionIfNotNull(true);

        // Assert
        _httpClient.DefaultRequestHeaders.Should()
            .HaveCount(1)
            .And
            .ContainKey("IsResubmission")
            .WhoseValue
            .Should()
            .BeEquivalentTo(true.ToString());
    }
}