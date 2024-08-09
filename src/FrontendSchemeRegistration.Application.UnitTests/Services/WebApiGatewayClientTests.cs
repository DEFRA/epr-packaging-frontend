namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Services;
using Application.Services.Interfaces;
using DTOs.Submission;
using Enums;
using FluentAssertions;
using FluentAssertions.Common;
using FrontendSchemeRegistration.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Moq;
using Moq.Protected;
using Options;

[TestFixture]
public class WebApiGatewayClientTests
{
    private const string SubmissionPeriod = "Jun to Dec 23";
    private readonly Mock<ITokenAcquisition> _tokenAcquisitionMock = new();
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private Mock<ILogger<WebApiGatewayClient>> _loggerMock;
    private IWebApiGatewayClient _webApiGatewayClient;
    private HttpClient _httpClient;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<WebApiGatewayClient>>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _webApiGatewayClient = new WebApiGatewayClient(
            _httpClient,
            _tokenAcquisitionMock.Object,
            Options.Create(new HttpClientOptions { UserAgent = "SchemeRegistration/1.0" }),
            Options.Create(new WebApiOptions { DownstreamScope = "https://api.com", BaseEndpoint = "https://example.com/" }),
            _loggerMock.Object);
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
            Array.Empty<byte>(), fileName, SubmissionPeriod, submissionId, submissionType, submissionSubType, registrationSetId);

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
            new SubmissionPeriodId
            {
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
        string queryString = $"lastTimeSync={new DateTime(2020, 1, 1)}";
        var submissionHistory = new List<SubmissionHistory>
        {
            new SubmissionHistory
            {
                SubmissionId = submissionId,
                FileName = "test.csv",
                UserName = "John Doe",
                SubmissionDate = new DateTime(2020, 8, 20),
                Status = "Accepted",
                DateofLatestStatusChange = new DateTime(2020, 9, 1)
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
            .Invoking(x => x.GetSubmissionHistoryAsync(Guid.NewGuid(), $"lastTimeSync={new DateTime(2020, 1, 1)}"))
            .Should()
            .ThrowAsync<HttpRequestException>();
        _loggerMock.VerifyLog(x => x.LogError("Error getting submission history"));
    }

    [Test]
    public async Task GetSubsidiariesAsync_ReturnsSubsidiaries_WhenResponseIsSuccessful()
    {
        // Arrange
        var expectedSubsidiaries = new List<SubsidiaryExportDto>
        {
            new SubsidiaryExportDto { Subsidiary_Id = 1, Organisation_Name = "Subsidiary 1" },
            new SubsidiaryExportDto { Subsidiary_Id = 2, Organisation_Name = "Subsidiary 2" }
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(expectedSubsidiaries)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _webApiGatewayClient.GetSubsidiariesAsync(123);

        // Assert
        result.Should().BeEquivalentTo(expectedSubsidiaries);
    }

    [Test]
    public async Task GetSubsidiariesAsync_ThrowsException_WhenResponseIsUnsuccessful()
    {
        // Arrange
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
        Func<Task> act = async () => await _webApiGatewayClient.GetSubsidiariesAsync(123);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}