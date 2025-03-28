namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Services;
using AutoFixture;
using DTOs.Submission;
using Enums;
using FluentAssertions;
using DTOs;
using DTOs.Prns;
using DTOs.Subsidiary.FileUploadStatus;
using DTOs.Subsidiary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Moq;
using Moq.Protected;
using Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using NUnit.Framework.Interfaces;

[TestFixture]
public class WebApiGatewayClientTests
{
    private const string SubmissionPeriod = "Jun to Dec 23";
    private readonly Mock<ITokenAcquisition> _tokenAcquisitionMock = new();
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<ILogger<WebApiGatewayClient>> _loggerMock;
    private WebApiGatewayClient _webApiGatewayClient;
    private HttpClient _httpClient;
    private Mock<IComplianceSchemeMemberService> _complianceSchemeMemberServiceMock;
    private static readonly IFixture _fixture = new Fixture();

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<WebApiGatewayClient>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _complianceSchemeMemberServiceMock = new Mock<IComplianceSchemeMemberService>();
        _webApiGatewayClient = new WebApiGatewayClient(
            _httpClient,
            _tokenAcquisitionMock.Object,
            Options.Create(new HttpClientOptions { UserAgent = "SchemeRegistration/1.0" }),
            Options.Create(new WebApiOptions { DownstreamScope = "https://api.com", BaseEndpoint = "https://example.com/" }),
            _loggerMock.Object,
            _complianceSchemeMemberServiceMock.Object // Pass the mocked service here
        );
    }


    [TearDown]
    public void Teardown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task UploadFileAsync_DoesNotThrowException_WhenUploadIsSuccessful()
    {
        // Arrange
        var byteArray = Array.Empty<byte>();
        const string fileName = "filename.csv";
        var submissionId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Headers = { { "Location", $"https://localhost:7265/api/v1/submissions/{submissionId}" } }
            });

        // Act
        Func<Task> action = async () => await _webApiGatewayClient.UploadFileAsync(byteArray, fileName, SubmissionPeriod, submissionId, SubmissionType.Producer);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Test]
    [TestCase(SubmissionType.Producer, null)]
    [TestCase(SubmissionType.Registration, SubmissionSubType.CompanyDetails)]
    [TestCase(SubmissionType.Registration, SubmissionSubType.Brands)]
    [TestCase(SubmissionType.Registration, SubmissionSubType.Partnerships)]
    public async Task UploadFileAsync_ShouldSetHttpHeaders(SubmissionType submissionType, SubmissionSubType? submissionSubType = null)
    {
        // Arrange
        const string fileName = "filename.csv";
        HttpRequestHeaders headers = _httpClient.DefaultRequestHeaders;
        var submissionId = Guid.NewGuid();
        var registrationSetId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Headers = { { "Location", $"/{submissionId}" } }
            });

        // Act
        await _webApiGatewayClient.UploadFileAsync(
            [], fileName, SubmissionPeriod, submissionId, submissionType, submissionSubType, registrationSetId);

        // Assert
        headers.GetValues("FileName").Single().Should().Be(fileName);
        headers.GetValues("SubmissionType").Single().Should().Be(submissionType.ToString());
        headers.GetValues("SubmissionId").Single().Should().Be(submissionId.ToString());
        headers.GetValues("RegistrationSetId").Single().Should().Be(registrationSetId.ToString());
        if (!submissionSubType.HasValue)
        {
            headers.Contains("SubmissionSubType").Should().BeFalse();
        }
        else
        {
            headers.GetValues("SubmissionSubType").Single().Should().Be(submissionSubType.Value.ToString());
        }
    }

    [Test]
    public async Task UploadFileAsync_ThrowsExceptions_WhenUploadIsNotSuccessful()
    {
        // Arrange
        var byteArray = Array.Empty<byte>();
        const string fileName = "filename.csv";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        // Act
        Func<Task> action = async () => await _webApiGatewayClient.UploadFileAsync(byteArray, fileName, SubmissionPeriod, null, SubmissionType.Producer);

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task UploadSubsidiaryFileAsync_DoesNotThrowException_WhenUploadIsSuccessful()
    {
        // Arrange
        var byteArray = Array.Empty<byte>();
        const string fileName = "filename.csv";
        var submissionId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Headers = { { "Location", $"https://localhost:7265/api/v1/submissions/{submissionId}" } }
            });

        // Act
        Func<Task> action = async () => await _webApiGatewayClient.UploadSubsidiaryFileAsync(byteArray, fileName, submissionId, SubmissionType.Subsidiary);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Test]
    [TestCase(SubmissionType.Subsidiary)]
    public async Task UploadSubsidiaryFileAsync_ShouldSetHttpHeaders(SubmissionType submissionType)
    {
        // Arrange
        const string fileName = "filename.csv";
        HttpRequestHeaders headers = _httpClient.DefaultRequestHeaders;
        var submissionId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Headers = { { "Location", $"/{submissionId}" } }
            });

        // Act
        await _webApiGatewayClient.UploadSubsidiaryFileAsync(
            [], fileName, submissionId, submissionType);

        // Assert
        headers.GetValues("FileName").Single().Should().Be(fileName);
        headers.GetValues("SubmissionType").Single().Should().Be(submissionType.ToString());
        headers.GetValues("SubmissionId").Single().Should().Be(submissionId.ToString());
    }

    [Test]
    public async Task UploadSubsidiaryFileAsync_ThrowsExceptions_WhenUploadIsNotSuccessful()
    {
        // Arrange
        var byteArray = Array.Empty<byte>();
        const string fileName = "filename.csv";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        // Act
        Func<Task> action = async () => await _webApiGatewayClient.UploadSubsidiaryFileAsync(byteArray, fileName, null, SubmissionType.Subsidiary);

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetSubmissionAsync_ReturnsSubmission_WhenSubmissionExists()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(new PomSubmission { Id = submissionId }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webApiGatewayClient.GetSubmissionAsync<PomSubmission>(submissionId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(submissionId);
    }

    [Test]
    public async Task GetSubmissionAsync_ReturnsNull_WhenResponseCodeIsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

        // Act
        var result = await _webApiGatewayClient.GetSubmissionAsync<PomSubmission>(submissionId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetSubmissionAsync_ThrowsException_WhenResponseCodeIsNotSuccessfulOr404()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetSubmissionAsync<PomSubmission>(submissionId))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting submission {id}", submissionId));
    }

    [Test]
    public async Task GetSubmissionsAsync_ThrowsException_WhenResponseCodeIsNotSuccessfulOr404()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetSubmissionsAsync<PomSubmission>("string"))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting submissions"));
    }

    [Test]
    public async Task GetSubmissionsAsync_ReturnsSubmissions_WhenCalled()
    {
        // Arrange
        const string queryString = "type=Producer&periods=Jan to Jun 23";
        var submissions = new List<PomSubmission>
        {
            new() { Id = Guid.NewGuid() }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(submissions)),
            });

        // Act
        var result = await _webApiGatewayClient.GetSubmissionsAsync<PomSubmission>(queryString);

        // Assert
        result.Should().BeEquivalentTo(submissions);
        const string expectedUri = "https://example.com/api/v1/submissions?type=Producer&periods=Jan to Jun 23";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task GetProducerValidationErrorsAsync_ThrowsException_WhenResponseCodeIsNotSuccessfulOr404()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetProducerValidationErrorsAsync(submissionId))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting producer validation records with submissionId: {Id}", submissionId));
    }

    [Test]
    public async Task SubmitAsync_DoesNotThrowException_WhenSubmitIsSuccessful()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var payload = new SubmissionPayload
        {
            FileId = Guid.NewGuid(),
            SubmittedBy = "Test Name"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act / Assert
        await _webApiGatewayClient.Invoking(x => x.SubmitAsync(submissionId, payload)).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/submissions/{submissionId}/submit";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsync_DoesNotThrowException_WhenSubmitIsSuccessfulAndSubmittedByIsNull()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act / Assert
        await _webApiGatewayClient.Invoking(x => x.SubmitAsync(submissionId, null)).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/submissions/{submissionId}/submit";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsync_ThrowsException_WhenSubmitIsNotSuccessful()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient.Invoking(x => x.SubmitAsync(submissionId, null)).Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task SubmitRegistrationApplication_DoesNotThrowException_WhenSubmitIsSuccessful()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var payload = new RegistrationApplicationPayload { Comments = "Pay part-payment of �24,500", ApplicationReferenceNumber = "PEPR00002125P1" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreateRegistrationApplicationEvent(submissionId, payload)).Should().NotThrowAsync();
        var expectedUri = $"https://example.com/api/v1/submissions/{submissionId}/submit-registration-application";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task SubmitRegistrationApplication_DoesNotThrowException_WhenSubmitIsSuccessfulAndPayloadIsNull()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreateRegistrationApplicationEvent(submissionId, null)).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/submissions/{submissionId}/submit-registration-application";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task SubmitRegistrationApplication_ThrowsException_WhenSubmitIsUnsuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient.Invoking(x => x.CreateRegistrationApplicationEvent(Guid.NewGuid(), null)).Should().ThrowAsync<HttpRequestException>();
    }
    
    [Test]
    public async Task GetDecisionsAsync_ReturnsDecisions_WhenCalled()
    {
        // Arrange
        const string queryString = "parameter=value";

        var decision = new PomDecision
        {
            SubmissionId = Guid.NewGuid()
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(decision)),
            });

        // Act
        var result = await _webApiGatewayClient.GetDecisionsAsync<PomDecision>(queryString);

        // Assert
        result.Should().NotBeNull();
        result.SubmissionId.Should().Be(decision.SubmissionId);
        const string expectedUri = $"https://example.com/api/v1/decisions?{queryString}";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task GetDecisionsAsync_ThrowsException_WhenErrorOccurs()
    {
        // Arrange
        const string queryString = "parameter=value";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Error"));

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetDecisionsAsync<PomDecision>(queryString))
            .Should()
            .ThrowAsync<HttpRequestException>();

        _loggerMock.VerifyLog(x => x.LogError(It.IsAny<Exception>(), "Error getting decision"), Times.Once);
    }

    [Test]
    public async Task GetRegistrationValidationErrorsAsync_ThrowsException_WhenResponseCodeIsNotSuccessfulOr404()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetRegistrationValidationErrorsAsync(submissionId))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting registration validation records with submissionId: {Id}", submissionId));
    }

    [Test]
    public async Task GetSubmissionIdsAsync_ReturnsSubmissionIds_WhenCalled()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        string queryString = $"type={SubmissionType.Producer}";
        var submissionIds = new List<SubmissionPeriodId>
        {
            new() {
                 SubmissionId = Guid.NewGuid(),
                 SubmissionPeriod = "July to January 2020",
                 Year = 2020
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(submissionIds)),
            });

        // Act
        var result = await _webApiGatewayClient.GetSubmissionIdsAsync(organisationId, queryString);

        // Assert
        result.Should().BeEquivalentTo(submissionIds);
        var expectedUri = $"https://example.com/api/v1/submissions/submission-Ids/{organisationId}?{queryString}";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task GetSubmissionIdsAsync_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetSubmissionIdsAsync(Guid.NewGuid(), $"type={SubmissionType.Producer}&year=2020"))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting submission ids"));
    }

    [Test]
    public async Task GetSubmissionHistoryAsync_ReturnsSubmissionHistory_WhenCalled()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        string queryString = $"lastTimeSync={new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)}";
        var submissionHistory = new List<SubmissionHistory>
        {
            new() {
                SubmissionId = submissionId,
                FileName = "test.csv",
                UserName = "John Doe",
                SubmissionDate = new DateTime(2020, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                Status = "Accepted",
                DateofLatestStatusChange = new DateTime(2020, 9, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(submissionHistory)),
            });

        // Act
        var result = await _webApiGatewayClient.GetSubmissionHistoryAsync(submissionId, queryString);

        // Assert
        result.Should().BeEquivalentTo(submissionHistory);
        var expectedUri = $"https://example.com/api/v1/submissions/submission-history/{submissionId}?{queryString}";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task GetSubmissionHistoryAsync_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetSubmissionHistoryAsync(Guid.NewGuid(), $"lastTimeSync={new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)}"))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting submission history"));
    }

    [Test]
    public async Task GetPrnsForLoggedOnUserAsync_ReturnsPrns_WhenSuccessful()
    {
        // Arrange
        var data = _fixture.CreateMany<PrnModel>().ToList();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(data)),
            });

        var result = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();

        result.Should().BeEquivalentTo(data);
    }

    [Test]
    public async Task GetPrnsForLoggedOnUserAsync_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetPrnsForLoggedOnUserAsync())
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetSearchPrnsAsync_ReturnsPrn_WhenSuccessful()
    {
        // Arrange
        var request = _fixture.Create<PaginatedRequest>();
        var data = _fixture.Create<PaginatedResponse<PrnModel>>();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(data)),
            });

        var result = await _webApiGatewayClient.GetSearchPrnsAsync(request);

        result.Should().BeEquivalentTo(data);
    }

    [Test]
    public async Task GetSearchPrnsAsync_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        var request = _fixture.Create<PaginatedRequest>();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetSearchPrnsAsync(request))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetPrnByExternalIdAsync_ReturnsPrn_WhenSuccessful()
    {
        // Arrange
        var data = _fixture.Create<PrnModel>();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(data)),
            });

        var result = await _webApiGatewayClient.GetPrnByExternalIdAsync(data.ExternalId);

        result.Should().BeEquivalentTo(data);
    }

    [Test]
    public async Task GetPrnByExternalIdAsync_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.GetPrnByExternalIdAsync(Guid.NewGuid()))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task SetPrnApprovalStatusToAcceptedAsyncForSinglePrn_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.SetPrnApprovalStatusToAcceptedAsync(Guid.NewGuid()))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task SetPrnApprovalStatusToAcceptedAsyncForSinglePrn_CallsFacadeWithCorrectPayload()
    {
        // Arrange
        var prnsToUpdate = Guid.NewGuid();

        HttpRequestMessage expectedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => expectedRequest = request)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act / Assert
        await _webApiGatewayClient.SetPrnApprovalStatusToAcceptedAsync(prnsToUpdate);

        expectedRequest.RequestUri.Should().Be("https://example.com/api/v1/prn/status");

        var body = await expectedRequest.Content.ReadFromJsonAsync<List<UpdatePrnStatus>>();

        body.Should().BeEquivalentTo(new List<UpdatePrnStatus>()
        {
            new() { PrnId = prnsToUpdate, Status = "ACCEPTED" }
        });
    }

    [Test]
    public async Task SetPrnApprovalStatusToAcceptedAsyncForMultiple_CallsFacadeWithCorrectPayload()
    {
        // Arrange
        var prnsToUpdate = new[] { Guid.NewGuid(), Guid.NewGuid() };

        HttpRequestMessage expectedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => expectedRequest = request)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act / Assert
        await _webApiGatewayClient.SetPrnApprovalStatusToAcceptedAsync(prnsToUpdate);

        expectedRequest.RequestUri.Should().Be("https://example.com/api/v1/prn/status");

        var body = await expectedRequest.Content.ReadFromJsonAsync<List<UpdatePrnStatus>>();

        body.Should().BeEquivalentTo(prnsToUpdate.Select(x => new UpdatePrnStatus { PrnId = x, Status = "ACCEPTED" }));
    }

    [Test]
    public async Task SetPrnApprovalStatusToAcceptedAsyncForMultiplePrn_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.SetPrnApprovalStatusToAcceptedAsync(new Guid[2]))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task SetPrnApprovalStatusToRejectedAsyncForSinglePrn_ThrowsException_WhenResponseCodeIsNotSuccessful()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act / Assert
        await _webApiGatewayClient
            .Invoking(x => x.SetPrnApprovalStatusToRejectedAsync(Guid.NewGuid()))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task SetPrnApprovalStatusToRejectedAsyncForSinglePrn_CallsFacadeWithCorrectPayload()
    {
        // Arrange
        var prnsToUpdate = Guid.NewGuid();

        HttpRequestMessage expectedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => expectedRequest = request)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act / Assert
        await _webApiGatewayClient.SetPrnApprovalStatusToRejectedAsync(prnsToUpdate);

        expectedRequest.RequestUri.Should().Be("https://example.com/api/v1/prn/status");

        var body = await expectedRequest.Content.ReadFromJsonAsync<List<UpdatePrnStatus>>();

        body.Should().BeEquivalentTo(new List<UpdatePrnStatus>()
        {
            new() { PrnId = prnsToUpdate, Status = "REJECTED" }
        });
    }

    [Test]
    public async Task GetRecyclingObligationsCalculation_Returns_ListOfPrnMaterialTableModel_OnSuccess()
    {
        // Arrange
        var year = 2023;
        var expectedNumberOfPrnsAwaitingAcceptance = 10;
        var externalIds = _fixture.CreateMany<Guid>().ToList();
        var prnMaterials = _fixture.CreateMany<PrnMaterialObligationModel>(7).ToList();
        var prnObligationModel = new PrnObligationModel
        {
            NumberOfPrnsAwaitingAcceptance = expectedNumberOfPrnsAwaitingAcceptance,
            ObligationData = prnMaterials
        };
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(prnObligationModel)),
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act
        var result = await _webApiGatewayClient.GetRecyclingObligationsCalculation(externalIds, year);

        // Assert
        result.Should().NotBeNull();
        result.NumberOfPrnsAwaitingAcceptance.Should().Be(expectedNumberOfPrnsAwaitingAcceptance);
        result.ObligationData.Count.Should().Be(prnMaterials.Count);

        // Asserting for first material
        var firstMaterial = result.ObligationData[0];
        var expectedFirstMaterial = prnMaterials[0];
        firstMaterial.MaterialName.Should().Be(expectedFirstMaterial.MaterialName);
        firstMaterial.ObligationToMeet.Should().Be(expectedFirstMaterial.ObligationToMeet);
        firstMaterial.TonnageAwaitingAcceptance.Should().Be(expectedFirstMaterial.TonnageAwaitingAcceptance);
        firstMaterial.TonnageAccepted.Should().Be(expectedFirstMaterial.TonnageAccepted);
        firstMaterial.TonnageOutstanding.Should().Be(expectedFirstMaterial.TonnageOutstanding);
        firstMaterial.Status.Should().Be(expectedFirstMaterial.Status);

        // Asserting for second material
        var secondMaterial = result.ObligationData[1];
        var expectedSecondMaterial = prnMaterials[1];
        secondMaterial.MaterialName.Should().Be(expectedSecondMaterial.MaterialName);
        secondMaterial.ObligationToMeet.Should().Be(expectedSecondMaterial.ObligationToMeet);
        secondMaterial.TonnageAwaitingAcceptance.Should().Be(expectedSecondMaterial.TonnageAwaitingAcceptance);
        secondMaterial.TonnageAccepted.Should().Be(expectedSecondMaterial.TonnageAccepted);
        secondMaterial.TonnageOutstanding.Should().Be(expectedSecondMaterial.TonnageOutstanding);
        secondMaterial.Status.Should().Be(expectedSecondMaterial.Status);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public void GetRecyclingObligationsCalculation_ThrowsException_OnFailure()
    {
        // Arrange
        var year = 2023;
		var externalIds = _fixture.CreateMany<Guid>().ToList();
		var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Verifiable();

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(() => _webApiGatewayClient.GetRecyclingObligationsCalculation(externalIds, year));

        Assert.That(exception.Message, Does.Contain("Response status code does not indicate success"));

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public async Task GetSubsidiaryUploadStatus_ReturnsDto_WhenResponseIsSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var expectedDto = new SubsidiaryUploadStatusDto
        {
            Status = SubsidiaryUploadStatus.Uploading
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(expectedDto)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webApiGatewayClient.GetSubsidiaryUploadStatus(userId, organisationId);

        // Assert
        result.Should().BeEquivalentTo(expectedDto);
    }

    [Test]
    public async Task GetSubsidiaryUploadStatus_ThrowsException_WhenResponseIsUnsuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        Func<Task> act = async () => await _webApiGatewayClient.GetSubsidiaryUploadStatus(userId, organisationId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [TestCase("C56A4180-65AA-42EC-A945-5FD21DEC0538", "D56A4180-65AA-42EC-A945-5FD21DEC0539")]
    public async Task GetSubsidiaryFileUploadStatusAsync_ShouldReturnUploadFileErrorResponse_WhenRequestIsSuccessful(string userIdStr, string organisationIdStr)
    {
        // Arrange
        var userId = Guid.Parse(userIdStr);
        var organisationId = Guid.Parse(organisationIdStr);
        var expectedResponse = new UploadFileErrorResponse();

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse)
            });

        // Act
        var result = await _webApiGatewayClient.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Test]
    public async Task GetRegistrationApplicationDetails_Should_Return_Details_When_Successful()
    {
        // Arrange
        var request = new GetRegistrationApplicationDetailsRequest
        {
            OrganisationId = Guid.NewGuid(),
            OrganisationNumber = 123,
            SubmissionPeriod = "2024-12",
            ComplianceSchemeId = Guid.NewGuid()
        };
        var expectedDetails = new RegistrationApplicationDetails
        {
            ApplicationReferenceNumber = "testRef",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(expectedDetails)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains($"OrganisationNumber={request.OrganisationNumber}") &&
                    req.RequestUri.ToString().Contains($"OrganisationId={request.OrganisationId}") &&
                    req.RequestUri.ToString().Contains($"SubmissionPeriod={request.SubmissionPeriod}") &&
                    req.RequestUri.ToString().Contains($"ComplianceSchemeId={request.ComplianceSchemeId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _webApiGatewayClient.GetRegistrationApplicationDetails(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDetails);
    }

    [Test]
    public async Task GetRegistrationApplicationDetails_Should_Return_Null_When_NoContent()
    {
        // Arrange
        var request = new GetRegistrationApplicationDetailsRequest
        {
            OrganisationId = Guid.NewGuid(),
            OrganisationNumber = 123,
            SubmissionPeriod = "2024-12"
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains($"OrganisationNumber={request.OrganisationNumber}") &&
                    req.RequestUri.ToString().Contains($"OrganisationId={request.OrganisationId}") &&
                    req.RequestUri.ToString().Contains($"SubmissionPeriod={request.SubmissionPeriod}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _webApiGatewayClient.GetRegistrationApplicationDetails(request);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetRegistrationApplicationDetails_Should_Log_Error_On_Exception()
    {
        // Arrange
        var request = new GetRegistrationApplicationDetailsRequest
        {
            OrganisationId = Guid.NewGuid(),
            OrganisationNumber = 123,
            SubmissionPeriod = "2024-12"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains($"OrganisationNumber={request.OrganisationNumber}") &&
                    req.RequestUri.ToString().Contains($"OrganisationId={request.OrganisationId}") &&
                    req.RequestUri.ToString().Contains($"SubmissionPeriod={request.SubmissionPeriod}")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Request failed"));

        // Act
        var result = await _webApiGatewayClient.GetRegistrationApplicationDetails(request);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(
            x => x.LogError(
                It.IsAny<Exception>(),
                It.Is<string>(msg => msg.Contains("Error Getting Registration Application Submission Details")),
                request.OrganisationId),
            Times.Once);
    }

    [Test]
    public async Task FileDownloadAsync_ShouldReturnFileData_WhenResponseIsSuccessful()
    {
        // Arrange
        Random rnd = new();
        byte[] data = new byte[10];
        rnd.NextBytes(data);
        var expectedData = data;

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedData))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _webApiGatewayClient.FileDownloadAsync(It.IsAny<string>());

        // Assert
        result.Should().NotBeEmpty();
    }

    [Test]
    public async Task FileDownloadAsync_ShouldThrowException_WhenResponseIsServerError()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        Func<Task> action = async () => await _webApiGatewayClient.FileDownloadAsync(It.IsAny<string>());

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>("an internal server error occurred");
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Should_Return_Details_When_Successful()
    {
        // Arrange
        var request = new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationId = Guid.NewGuid(),
            OrganisationNumber = 123,
            SubmissionPeriod = "2024-12",
            ComplianceSchemeId = Guid.NewGuid()
        };
        var expectedDetails = new PackagingResubmissionApplicationDetails
        {
            ApplicationReferenceNumber = "testref",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(expectedDetails)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains($"OrganisationNumber={request.OrganisationNumber}") &&
                    req.RequestUri.ToString().Contains($"OrganisationId={request.OrganisationId}") &&
                    req.RequestUri.ToString().Contains($"SubmissionPeriod={request.SubmissionPeriod}") &&
                    req.RequestUri.ToString().Contains($"ComplianceSchemeId={request.ComplianceSchemeId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _webApiGatewayClient.GetPackagingDataResubmissionApplicationDetails(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDetails);
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Should_Return_Null_When_NoContent()
    {
        // Arrange
        var request = new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationId = Guid.NewGuid(),
            OrganisationNumber = 123,
            SubmissionPeriod = "2024-12"
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains($"OrganisationNumber={request.OrganisationNumber}") &&
                    req.RequestUri.ToString().Contains($"OrganisationId={request.OrganisationId}") &&
                    req.RequestUri.ToString().Contains($"SubmissionPeriod={request.SubmissionPeriod}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _webApiGatewayClient.GetPackagingDataResubmissionApplicationDetails(request);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Should_Log_Error_On_Exception()
    {
        // Arrange
        var request = new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationId = Guid.NewGuid(),
            OrganisationNumber = 123,
            SubmissionPeriod = "2024-12"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains($"OrganisationNumber={request.OrganisationNumber}") &&
                    req.RequestUri.ToString().Contains($"OrganisationId={request.OrganisationId}") &&
                    req.RequestUri.ToString().Contains($"SubmissionPeriod={request.SubmissionPeriod}")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Request failed"));

        // Act
        var result = await _webApiGatewayClient.GetPackagingDataResubmissionApplicationDetails(request);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationDetails_SuccessfulResponse_ReturnsMemberDetails()
    {
        // Arrange
        var request = new PackagingResubmissionMemberRequest();
        var expectedResponse = new PackagingResubmissionMemberDetails { MemberCount = 0 };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse)
            });

        // Act
        var result = await _webApiGatewayClient.GetPackagingResubmissionMemberDetails(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationDetails_NoContent_ReturnsNull()
    {
        // Arrange
        var request = new PackagingResubmissionMemberRequest();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _webApiGatewayClient.GetPackagingResubmissionMemberDetails(request);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationDetails_HttpRequestException_ReturnsNull()
    {
        // Arrange
        var request = new PackagingResubmissionMemberRequest();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await _webApiGatewayClient.GetPackagingResubmissionMemberDetails(request);

        // Assert
        result.Should().BeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationDetails_HttpRequestException_PreConditionRequired_ThrowsException()
    {
        // Arrange
        var request = new PackagingResubmissionMemberRequest();
        var testMessage = "This is a test message";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.PreconditionRequired, ReasonPhrase = testMessage });

        // Act
        var ex = Assert.ThrowsAsync<HttpRequestException>(() => _webApiGatewayClient.GetPackagingResubmissionMemberDetails(request));

        // Assert
        Assert.That(ex != null);
        Assert.That(ex.GetType() == typeof(HttpRequestException));
        Assert.That(ex.Message.Contains(testMessage));
    }

    [Test]
	public async Task CreatePackagingResubmissionReferenceNumberEvent_DoesNotThrowException_WhenSubmitIsSuccessful()
	{
		// Arrange
		var submissionId = Guid.NewGuid();
		var @event = new PackagingResubmissionReferenceNumberCreatedEvent { PackagingResubmissionReferenceNumber = "test-reference" };

		_httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

		// Act & Assert
		await _webApiGatewayClient.Invoking(x => x.CreatePackagingResubmissionReferenceNumberEvent(submissionId, @event)).Should().NotThrowAsync();

		var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-reference-number-event";
		_httpMessageHandlerMock.Protected().Verify(
			"SendAsync", Times.Once(),
			ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
			ItExpr.IsAny<CancellationToken>());
	}

	[Test]
	public async Task WhenSubmitIsSuccessful_CreatePackagingResubmissionReferenceNumberEvent_DoesNotThrowException()
	{
		// Arrange
		var submissionId = Guid.NewGuid();
		var @event = new PackagingResubmissionReferenceNumberCreatedEvent { PackagingResubmissionReferenceNumber = "test-reference" };

		_httpMessageHandlerMock.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

		// Act & Assert
		await _webApiGatewayClient.Invoking(x => x.CreatePackagingResubmissionReferenceNumberEvent(submissionId, @event)).Should().NotThrowAsync();

		var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-reference-number-event";
		_httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
			ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
			ItExpr.IsAny<CancellationToken>());
	}

    [Test]
    public async Task CreatePackagingResubmissionFeeViewEvent_DoesNotThrowException_WhenSubmitIsSuccessful()
    {
        // Arrange
        var submissionId = Guid.NewGuid();       
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreatePackagingResubmissionFeeViewEvent(submissionId)).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-fee-view-event";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task WhenSubmitIsSuccessful_CreatePackagingResubmissionFeeViewEvent_DoesNotThrowException()
    {
        // Arrange
        var submissionId = Guid.NewGuid();      
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreatePackagingResubmissionFeeViewEvent(submissionId)).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-fee-view-event";
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task CreatePackagingDataResubmissionFeePaymentEvent_DoesNotThrowException_WhenSubmitIsSuccessful()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var filedId = Guid.NewGuid();
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId,"payment")).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-fee-payment-event";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task WhenSubmitIsSuccessful_CreatePackagingDataResubmissionFeePaymentEvent_DoesNotThrowException()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var filedId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId,"payment")).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-fee-payment-event";
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent_DoesNotThrowException_WhenSubmitIsSuccessful()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var filedId = Guid.NewGuid();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId,"submittedBy",DateTime.Today,"Comment")).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-application-submitted-event";
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task WhenSubmitIsSuccessful_CreatePackagingResubmissionApplicationSubmittedCreatedEvent_DoesNotThrowException()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var filedId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act & Assert
        await _webApiGatewayClient.Invoking(x => x.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId, "submittedBy", DateTime.Today, "Comment")).Should().NotThrowAsync();

        var expectedUri = $"https://example.com/api/v1/packaging-resubmission/{submissionId}/create-packaging-resubmission-application-submitted-event";
        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString() == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }
}