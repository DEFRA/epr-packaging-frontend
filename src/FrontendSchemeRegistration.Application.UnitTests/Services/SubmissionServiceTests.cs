using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Moq;

namespace FrontendSchemeRegistration.Application.UnitTests.Services;

[TestFixture]
public class SubmissionServiceTests
{
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private ISubmissionService _submissionService;

    [SetUp]
    public void SetUp()
    {
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _submissionService = new SubmissionService(_webApiGatewayClientMock.Object);
    }

    [Test]
    public async Task GetSubmissionAsync_CallsClient_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        // Act
        await _submissionService.GetSubmissionAsync<PomSubmission>(submissionId);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionAsync<PomSubmission>(submissionId), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenSubmissionPeriodsListIsEmpty()
    {
        // Arrange
        var dataPeriods = new List<string>();

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null, null);

        // Assert
        const string expectedQueryString = "type=Producer";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenSubmissionPeriodsListHasOneSubmissionPeriod()
    {
        // Arrange
        var dataPeriods = new List<string>
        {
            "Jan to Jun 23"
        };

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null, null);

        // Assert
        const string expectedQueryString = "type=Producer&periods=Jan+to+Jun+23";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenSubmissionPeriodsListHasMultipleSubmissionPeriod()
    {
        // Arrange
        var dataPeriods = new List<string>
        {
            "Jan to Jun 23",
            "Jul to Dec 23"
        };

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null, null);

        // Assert
        const string expectedQueryString = "type=Producer&periods=Jan+to+Jun+23%2cJul+to+Dec+23";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenLimitIsPassed()
    {
        // Arrange
        var dataPeriods = new List<string>();

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, 2, null, null);

        // Assert
        const string expectedQueryString = "type=Producer&limit=2";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenComplianceSchemeIdIsPassed()
    {
        // Arrange
        var complianceSchemeId = Guid.NewGuid();
        var dataPeriods = new List<string>();

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, complianceSchemeId, null);

        // Assert
        var expectedQueryString = $"type=Producer&complianceSchemeId={complianceSchemeId}";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenIsComplianceSchemeFirstIsPassed()
    {
        // Arrange
        var dataPeriods = new List<string>();

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null, true);

        // Assert
        var expectedQueryString = $"type=Producer&isFirstComplianceScheme=True";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionsAsync_CallsClientWithCorrectQueryString_WhenAllParametersArePassed()
    {
        // Arrange
        var complianceSchemeId = Guid.NewGuid();
        var dataPeriods = new List<string>
        {
            "Jan to Jun 23",
            "Jul to Dec 23"
        };

        // Act
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, 2, complianceSchemeId, true);

        // Assert
        var expectedQueryString = $"type=Producer&periods=Jan+to+Jun+23%2cJul+to+Dec+23&limit=2&complianceSchemeId={complianceSchemeId}&isFirstComplianceScheme=True";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionsAsync<PomSubmission>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task SubmitAsync_CallsClient_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Act
        await _submissionService.SubmitAsync(submissionId, fileId);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.SubmitAsync(submissionId, It.IsAny<SubmissionPayload>()), Times.Once);
    }

    [Test]
    public async Task SubmitAsyncIncludingSubmittedBy_CallsClient_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        const string submittedBy = "TestName";

        // Act
        await _submissionService.SubmitAsync(submissionId, fileId, submittedBy);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.SubmitAsync(submissionId, It.IsAny<SubmissionPayload>()), Times.Once);
    }

    [Test]
    public async Task GetDecisionAsync_CallsClientWithCorrectQueryString_WhenAllParametersArePassed()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var limit = 10;

        // Act
        await _submissionService.GetDecisionAsync<PomDecision>(limit, submissionId);

        // Assert
        var expectedQueryString = $"limit=10&submissionId={submissionId}";
        _webApiGatewayClientMock.Verify(x => x.GetDecisionsAsync<PomDecision>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetDecisionAsync_CallsClientWithCorrectQueryString_WhenSubmissionIdIsPassed()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        // Act
        await _submissionService.GetDecisionAsync<PomDecision>(null, submissionId);

        // Assert
        var expectedQueryString = $"submissionId={submissionId}";
        _webApiGatewayClientMock.Verify(x => x.GetDecisionsAsync<PomDecision>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetDecisionAsync_CallsClientWithCorrectQueryString_WhenLimitIsPassed()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var limit = 1;

        // Act
        await _submissionService.GetDecisionAsync<PomDecision>(limit, submissionId);

        // Assert
        var expectedQueryString = $"limit=1&submissionId={submissionId}";
        _webApiGatewayClientMock.Verify(x => x.GetDecisionsAsync<PomDecision>(expectedQueryString), Times.Once);
    }
}