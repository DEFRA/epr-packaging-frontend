namespace FrontendSchemeRegistration.Application.UnitTests.Services.ComplianceScheme;

using System;
using System.Net;
using System.Threading.Tasks;
using Application.Services;
using Application.Services.Interfaces;
using DTOs.ComplianceScheme;
using DTOs.UserAccount;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Moq;
using Moq.Protected;
using Options;

[TestFixture]
public class ComplianceSchemeServiceTests : ServiceTestBase<IComplianceSchemeService>
{
    private const string ServiceError = "Service error";

    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<ILogger<ComplianceSchemeService>> _loggerMock = new();
    private readonly Mock<ITokenAcquisition> _tokenAcquisitionMock = new();
    private readonly List<ProducerComplianceSchemeDto> _complianceSchemeDtos = new();
    private readonly Guid _complianceSchemeId = new Guid("{00000000-0000-0000-0000-000000000001}");

    private ComplianceSchemeService _complianceSchemeService;
    private Organisation _organisation;

    [SetUp]
    public void Setup()
    {
        _organisation = UserAccount.Organisations.SingleOrDefault(x => x.OrganisationRole == OrganisationRoles.Producer);
    }

    [Test]
    public async Task GetAllComplianceSchemes_ReturnsSchemes()
    {
        _complianceSchemeDtos.Add(SelectedComplianceScheme);
        _complianceSchemeDtos.Add(CurrentComplianceScheme);

        var stringContent = _complianceSchemeDtos.ToJsonContent();
        _complianceSchemeService = MockService(HttpStatusCode.OK, stringContent);

        var result = await _complianceSchemeService.GetComplianceSchemes();
        var complianceSchemeDtos = result.ToList();
        complianceSchemeDtos.AsEnumerable().Count().Should().Be(2);
    }

    [Test]
    public async Task GetAllComplianceSchemes_ReturnsException()
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.InternalServerError, null, true);

        // Act
        Func<Task<IEnumerable<ComplianceSchemeDto>?>> func = async () => await _complianceSchemeService.GetComplianceSchemes();

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain(ServiceError);
    }

    [Test]
    public async Task GetProducerComplianceScheme_ReturnsOk()
    {
        var stringContent = SelectedComplianceScheme.ToJsonContent();

        _complianceSchemeService = MockService(HttpStatusCode.OK, stringContent);

        var result = await _complianceSchemeService.GetProducerComplianceScheme(_organisation.Id);
        result.ComplianceSchemeId.Should().Be(SelectedComplianceScheme.ComplianceSchemeId);
    }

    [Test]
    // Not happy with the test name but following the pattern here. Should have been GetProducerComplianceScheme_WhenSchemeNotfound_ReturnsNoContent
    public async Task GetProducerComplianceScheme_ReturnsNoContent()    
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.NoContent, null);

        // Act
        var result = await _complianceSchemeService.GetProducerComplianceScheme(_organisation.Id);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetProducerComplianceScheme_ReturnsException()
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.InternalServerError, null, true);

        // Act
        Func<Task<ProducerComplianceSchemeDto?>> func = async () => await _complianceSchemeService.GetProducerComplianceScheme(It.IsAny<Guid>());

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain(ServiceError);
    }

    [Test]
    public async Task GetOperatorComplianceSchemes_ReturnsOk()
    {
        var complianceSchemes = new List<ComplianceSchemeDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "ComplianceScheme1"
            }
        };
        var stringContent = complianceSchemes.ToJsonContent();

        _complianceSchemeService = MockService(HttpStatusCode.OK, stringContent);

        var result = await _complianceSchemeService.GetOperatorComplianceSchemes(_organisation.Id);
        result.First().Id.Should().Be(complianceSchemes[0].Id);
    }

    [Test]
    public async Task GetOperatorComplianceSchemes_ReturnsException()
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.InternalServerError, null, true);

        // Act
        Func<Task<IEnumerable<ComplianceSchemeDto>>> func = async () => await _complianceSchemeService.GetOperatorComplianceSchemes(It.IsAny<Guid>());

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain(ServiceError);
    }

    [Test]
    public async Task StopComplianceScheme_ReturnsOk()
    {
        _complianceSchemeService = MockService(HttpStatusCode.OK, null);

        var result = await _complianceSchemeService.StopComplianceScheme(CurrentComplianceScheme.ComplianceSchemeId, Guid.NewGuid());
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task StopComplianceScheme_ReturnsException()
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.InternalServerError, null, true);

        // Act
        Func<Task<HttpResponseMessage>> func = async () => await _complianceSchemeService.StopComplianceScheme(
            It.IsAny<Guid>(),
            It.IsAny<Guid>());

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain(ServiceError);
    }

    [Test]
    public async Task ConfirmAddComplianceScheme_ReturnsOk()
    {
        var parameters = new
        {
            id = _complianceSchemeId,
        };
        var stringContent = parameters.ToJsonContent();
        _complianceSchemeService = MockService(HttpStatusCode.OK, stringContent);

        var result = await _complianceSchemeService.ConfirmAddComplianceScheme(
            SelectedComplianceScheme.ComplianceSchemeId,
            _organisation.Id);

        result.Id.Should().Be(_complianceSchemeId);
    }

    [Test]
    public async Task ConfirmAddComplianceScheme_ReturnsException()
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.InternalServerError, null, true);

        // Act
        Func<Task<SelectedSchemeDto?>> func = async () => await _complianceSchemeService.ConfirmAddComplianceScheme(
            It.IsAny<Guid>(),
            It.IsAny<Guid>());

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain(ServiceError);
    }

    [Test]
    public async Task ConfirmUpdateComplianceScheme_ReturnsOk()
    {
        var parameters = new
        {
            id = _complianceSchemeId,
        };
        var stringContent = parameters.ToJsonContent();
        _complianceSchemeService = MockService(HttpStatusCode.OK, stringContent);

        var result = await _complianceSchemeService.ConfirmUpdateComplianceScheme(
            SelectedComplianceScheme.ComplianceSchemeId,
            CurrentComplianceScheme.ComplianceSchemeId,
            _organisation.Id);

        result.Id.Should().Be(_complianceSchemeId);
    }

    [Test]
    public async Task ConfirmUpdateComplianceScheme_ReturnsException()
    {
        // Arrange
        _complianceSchemeService = MockService(HttpStatusCode.InternalServerError, null, true);

        // Act
        Func<Task<SelectedSchemeDto?>> func = async () => await _complianceSchemeService.ConfirmUpdateComplianceScheme(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>());

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain(ServiceError);
    }

    [Test]
    public async Task ClearSummaryCache_WithoutCache_Returns()
    {
        // Arrange
        var client = new HttpClient(_httpMessageHandlerMock.Object);
        client.BaseAddress = new Uri("https://mock/api/test/");
        client.Timeout = TimeSpan.FromSeconds(30);

        var facadeOptions = Options.Create(new AccountsFacadeApiOptions { DownstreamScope = "https://mock/test" });
        var accountServiceApiClient = new AccountServiceApiClient(client, _tokenAcquisitionMock.Object, facadeOptions);
        var cachingOptions = Options.Create(new CachingOptions { CacheComplianceSchemeSummaries = true });

        _complianceSchemeService =
            new ComplianceSchemeService(accountServiceApiClient, _loggerMock.Object, cachingOptions, null);

        // Act
        await _complianceSchemeService.ClearSummaryCache(
            It.IsAny<Guid>(),
            It.IsAny<Guid>());

        // Assert
        _complianceSchemeService.HasCache().Should().BeFalse();
    }

    [Test]
    public async Task ClearSummaryCache_WithCache_Removes_OrganisationID_And_ComplianceSchemeID()
    {
        // Arrange
        var client = new HttpClient(_httpMessageHandlerMock.Object);
        client.BaseAddress = new Uri("https://mock/api/test/");
        client.Timeout = TimeSpan.FromSeconds(30);

        var facadeOptions = Options.Create(new AccountsFacadeApiOptions { DownstreamScope = "https://mock/test" });
        var accountServiceApiClient = new AccountServiceApiClient(client, _tokenAcquisitionMock.Object, facadeOptions);
        var cachingOptions = Options.Create(new CachingOptions { CacheComplianceSchemeSummaries = true, SlidingExpirationSeconds = 1000, AbsoluteExpirationSeconds = 2000 });

        _complianceSchemeService =
            new ComplianceSchemeService(accountServiceApiClient, _loggerMock.Object, cachingOptions, new Mock<IDistributedCache>().Object);

        // Act
        await _complianceSchemeService.ClearSummaryCache(
            It.IsAny<Guid>(),
            It.IsAny<Guid>());

        // Assert
        _complianceSchemeService.HasCache().Should().BeTrue();
    }

    protected override ComplianceSchemeService MockService(HttpStatusCode expectedStatusCode, HttpContent expectedContent, bool raiseServiceException = false)
    {
        if (raiseServiceException)
        {
            _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception(ServiceError));
        }
        else
        {
            _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = expectedStatusCode,
                Content = expectedContent,
            });
        }

        var client = new HttpClient(_httpMessageHandlerMock.Object);
        client.BaseAddress = new Uri("https://mock/api/test/");
        client.Timeout = TimeSpan.FromSeconds(30);

        var facadeOptions = Options.Create(new AccountsFacadeApiOptions { DownstreamScope = "https://mock/test" });
        var accountServiceApiClient = new AccountServiceApiClient(client, _tokenAcquisitionMock.Object, facadeOptions);
        var cachingOptions = Options.Create(new CachingOptions { CacheComplianceSchemeSummaries = false });

        return new ComplianceSchemeService(accountServiceApiClient, _loggerMock.Object, cachingOptions, new Mock<IDistributedCache>().Object);
    }
}