namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.RegistrationSubmission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[TestFixture]
public class RegistrationSubmissionDataServiceTests
{
    private const string Endpoint = "registration-submission-data";

    private Mock<IPaymentCalculationServiceApiClient> _apiClientMock;
    private RegistrationSubmissionDataService _sut;

    [SetUp]
    public void SetUp()
    {
        _apiClientMock = new Mock<IPaymentCalculationServiceApiClient>();
        var options = Options.Create(new PaymentFacadeApiOptions
        {
            Endpoints = new PaymentFacadeApiEndpoints { RegistrationSubmissionDataEndpoint = Endpoint },
        });
        _sut = new RegistrationSubmissionDataService(_apiClientMock.Object, options, NullLogger<RegistrationSubmissionDataService>.Instance);
    }

    [Test]
    public async Task NotifyAsync_PostsExpectedEndpointAndBody()
    {
        var request = NewRequest();
        _apiClientMock
            .Setup(c => c.SendPostRequest(Endpoint, It.IsAny<CreateRegistrationSubmissionDataRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await _sut.NotifyAsync(request);

        _apiClientMock.Verify(
            c => c.SendPostRequest(Endpoint,
                It.Is<CreateRegistrationSubmissionDataRequest>(r =>
                    r.SubmissionId == request.SubmissionId &&
                    r.FileId == request.FileId &&
                    r.ComplianceSchemeId == request.ComplianceSchemeId &&
                    r.SubmissionPeriod == request.SubmissionPeriod &&
                    r.SubmissionDate == request.SubmissionDate)),
            Times.Once);
    }

    [Test]
    public async Task NotifyAsync_NullRequest_DoesNothing()
    {
        await _sut.NotifyAsync(null);

        _apiClientMock.Verify(
            c => c.SendPostRequest(It.IsAny<string>(), It.IsAny<CreateRegistrationSubmissionDataRequest>()),
            Times.Never);
    }

    [Test]
    public async Task NotifyAsync_ApiThrows_SwallowsAndLogs()
    {
        var request = NewRequest();
        _apiClientMock
            .Setup(c => c.SendPostRequest(It.IsAny<string>(), It.IsAny<CreateRegistrationSubmissionDataRequest>()))
            .ThrowsAsync(new HttpRequestException("facade down"));

        Func<Task> act = () => _sut.NotifyAsync(request);

        await act.Should().NotThrowAsync();
    }

    private static CreateRegistrationSubmissionDataRequest NewRequest() => new()
    {
        SubmissionId = Guid.NewGuid(),
        FileId = Guid.NewGuid(),
        ComplianceSchemeId = Guid.NewGuid(),
        SubmissionPeriod = "Jan to Jun 2026",
        SubmissionDate = new DateTime(2026, 5, 29, 9, 0, 0, DateTimeKind.Utc),
    };
}
