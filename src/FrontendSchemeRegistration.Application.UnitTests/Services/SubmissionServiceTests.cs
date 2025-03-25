using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Moq;

namespace FrontendSchemeRegistration.Application.UnitTests.Services;

[TestFixture]
public class SubmissionServiceTests
{
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private SubmissionService _submissionService;

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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null);

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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null);

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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null);

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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, 2, null);

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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, complianceSchemeId);

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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, null, null);

        // Assert
        var expectedQueryString = $"type=Producer";
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
        await _submissionService.GetSubmissionsAsync<PomSubmission>(dataPeriods, 2, complianceSchemeId);

        // Assert
        var expectedQueryString = $"type=Producer&periods=Jan+to+Jun+23%2cJul+to+Dec+23&limit=2&complianceSchemeId={complianceSchemeId}";
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
    public async Task CreateApplicationReferenceNumber_CallsClient_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Act
        await _submissionService.SubmitAsync(submissionId, fileId, "TestSubmittedBy", "TestReference", true);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.SubmitAsync(submissionId, It.Is<SubmissionPayload>(p => p.IsResubmission == true && p.SubmittedBy == "TestSubmittedBy" && p.AppReferenceNumber == "TestReference")), Times.Once);
    }

    [Test]
    public async Task SubmitAsyncIncludingSubmittedBy_CallsClient_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        const string submittedBy = "TestName";

        // Act
        await _submissionService.SubmitAsync(submissionId, fileId, submittedBy, null, false);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.SubmitAsync(submissionId, It.IsAny<SubmissionPayload>()), Times.Once);
    }

    [Test]
    public async Task WhenSubmitAsyncIsCalledToSaveSubmissionType_CallsWebApiGatewayClient()
    {
        // Arrange
        var reference = "PEPR00002125P1";
        var complianceSchemeId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var submissionType = SubmissionType.RegistrationFeePayment;
        var paymentMethod = "test";
        var comment = "test";

        // Act
        await _submissionService.CreateRegistrationApplicationEvent(submissionId, complianceSchemeId, comment, paymentMethod, reference, false, submissionType);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.CreateRegistrationApplicationEvent(submissionId, It.Is<RegistrationApplicationPayload>(p =>
            p.ApplicationReferenceNumber == reference &&
            p.SubmissionType == submissionType &&
            p.ComplianceSchemeId == complianceSchemeId &&
            p.Comments == comment &&
            p.PaymentMethod == paymentMethod)), Times.Once);
    }

    [Test]
    public async Task WhenSubmitRegistrationApplicationAsyncIsInvoked_CallsWebApiGatewayClient()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        const string comments = "Pay part-payment of £24,500 now";
        const string applicationReference = "PEPR00002125P1";

        // Act
        await _submissionService.CreateRegistrationApplicationEvent(submissionId, null, comments, null, applicationReference, false, SubmissionType.RegistrationApplicationSubmitted);

        // Assert
        _webApiGatewayClientMock.Verify(x => x.CreateRegistrationApplicationEvent(submissionId, It.IsAny<RegistrationApplicationPayload>()), Times.Once);
    }

    [Test]
    public async Task GetDecisionAsync_CallsClientWithCorrectQueryString_WhenAllParametersArePassed()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var limit = 10;
        var type = SubmissionType.Producer;

        // Act
        await _submissionService.GetDecisionAsync<PomDecision>(limit, submissionId, type);

        // Assert
        var expectedQueryString = $"limit=10&submissionId={submissionId}&type={type}";
        _webApiGatewayClientMock.Verify(x => x.GetDecisionsAsync<PomDecision>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetDecisionAsync_CallsClientWithCorrectQueryString_WhenSubmissionIdIsPassed()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var type = SubmissionType.Producer;

        // Act
        await _submissionService.GetDecisionAsync<PomDecision>(null, submissionId, type);

        // Assert
        var expectedQueryString = $"submissionId={submissionId}&type={type}";
        _webApiGatewayClientMock.Verify(x => x.GetDecisionsAsync<PomDecision>(expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetDecisionAsync_CallsClientWithCorrectQueryString_WhenLimitIsPassed()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var limit = 1;
        var type = SubmissionType.Producer;

        // Act
        await _submissionService.GetDecisionAsync<PomDecision>(limit, submissionId, type);

        // Assert
        var expectedQueryString = $"limit=1&submissionId={submissionId}&type={type}";
        _webApiGatewayClientMock.Verify(x => x.GetDecisionsAsync<PomDecision>(expectedQueryString), Times.Once);
    }

    [Test]
    [TestCase(SubmissionType.Producer)]
    [TestCase(SubmissionType.Registration)]
    public async Task GetSubmissionIdsAsync_CallsClientWithCorrectQueryString_WhenCalled(SubmissionType type)
    {
        // Arrange
        var organisationId = Guid.NewGuid();

        // Act
        await _submissionService.GetSubmissionIdsAsync(organisationId, type, null, null);

        // Assert
        var expectedQueryString = $"type={type}";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionIdsAsync(organisationId, expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionIdsAsync_CallsClientWithCorrectQueryString_WhenCalledWithComplianceSchemeIdAndYear()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var type = SubmissionType.Producer;
        var complianceSchemeId = Guid.NewGuid();
        var year = 2020;

        // Act
        await _submissionService.GetSubmissionIdsAsync(organisationId, type, complianceSchemeId, year);

        // Assert
        var expectedQueryString = $"type={type}&complianceSchemeId={complianceSchemeId}&year={year}";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionIdsAsync(organisationId, expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetSubmissionHistoryAsync_CallsClientWithCorrectQueryString_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var lastSyncTime = new DateTime(2020, 1, 1);

        // Act
        await _submissionService.GetSubmissionHistoryAsync(submissionId, lastSyncTime);

        // Assert
        var expectedQueryString = $"lastSyncTime={lastSyncTime:s}";
        _webApiGatewayClientMock.Verify(x => x.GetSubmissionHistoryAsync(submissionId, expectedQueryString), Times.Once);
    }

    [Test]
    public async Task GetRegistrationApplicationDetails_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var request = new GetRegistrationApplicationDetailsRequest();

        // Act
        await _submissionService.GetRegistrationApplicationDetails(request);

        // Assert

        _webApiGatewayClientMock.Verify(x => x.GetRegistrationApplicationDetails(request), Times.Once);
    }


    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var request = new GetPackagingResubmissionApplicationDetailsRequest();

        // Act
        await _submissionService.GetPackagingDataResubmissionApplicationDetails(request);

        // Assert

        _webApiGatewayClientMock.Verify(x => x.GetPackagingDataResubmissionApplicationDetails(request), Times.Once);
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationDetails_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var request = new PackagingResubmissionMemberRequest();

        // Act
        await _submissionService.GetPackagingResubmissionMemberDetails(request);

        // Assert

        _webApiGatewayClientMock.Verify(x => x.GetPackagingResubmissionMemberDetails(request), Times.Once);
    }

    [Test]
    public async Task CreatePackagingResubmissionReferenceNumberEvent_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var request = new PackagingResubmissionReferenceNumberCreatedEvent();

        // Act
        await _submissionService.CreatePackagingResubmissionReferenceNumberEvent(submissionId, request);

        // Assert

        _webApiGatewayClientMock.Verify(x => x.CreatePackagingResubmissionReferenceNumberEvent(submissionId,request), Times.Once);
    }

    [Test]
    public async Task CreatePackagingResubmissionFeeViewEvent_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        
        // Act
        await _submissionService.CreatePackagingResubmissionFeeViewEvent(submissionId);

        // Assert

        _webApiGatewayClientMock.Verify(x => x.CreatePackagingResubmissionFeeViewEvent(submissionId), Times.Once);
    }

    [Test]
    public async Task CreatePackagingDataResubmissionFeePaymentEvent_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var filedId = Guid.NewGuid();
       

        // Act
        await _submissionService.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId,"paymentMethod");

        // Assert

        _webApiGatewayClientMock.Verify(x => x.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId,"paymentMethod"), Times.Once);
    }

    [Test]
    public async Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent_CallsClient_WithCorrectQueryString()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var filedId = Guid.NewGuid();


        // Act
        await _submissionService.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId,"submittedBy",DateTime.Today,"Comment");

        // Assert

        _webApiGatewayClientMock.Verify(x => x.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId, "submittedBy", DateTime.Today, "Comment"), Times.Once);
    }

}