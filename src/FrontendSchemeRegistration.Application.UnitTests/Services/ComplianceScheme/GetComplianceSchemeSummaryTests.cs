namespace FrontendSchemeRegistration.Application.UnitTests.Services.ComplianceScheme;

using System.Net;
using System.Text.Json;
using DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Options;
using UI.Extensions;

[TestFixture]
public class GetComplianceSchemeSummaryTests
{
    private readonly Mock<IAccountServiceApiClient> _accountServiceApiClient = new();
    private ComplianceSchemeService _complianceSchemeService;

    [SetUp]
    public void Setup()
    {
        var memoryDistributedCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
        IDistributedCache cache = new MemoryDistributedCache(memoryDistributedCacheOptions);

        _accountServiceApiClient.Invocations.Clear();

        var cachingOptions = Options.Create(
            new CachingOptions { SlidingExpirationSeconds = 120, AbsoluteExpirationSeconds = 300, CacheComplianceSchemeSummaries = true });

        _complianceSchemeService = new ComplianceSchemeService(
            _accountServiceApiClient.Object, NullLogger<ComplianceSchemeService>.Instance, cachingOptions, cache);
    }

    [Test]
    public async Task WhenCacheRecordIsMissing_ThenApiRequestIsSentToSpecificUrl()
    {
        var operatorOrganisationId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();

        _accountServiceApiClient
            .Setup(client => client.SendGetRequest(operatorOrganisationId, It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new ComplianceSchemeSummary()))
            });

        await _complianceSchemeService.GetComplianceSchemeSummary(operatorOrganisationId, complianceSchemeId);

        var expectedEndpoint = $"compliance-schemes/{complianceSchemeId}/summary";

        _accountServiceApiClient.Verify(client => client.SendGetRequest(operatorOrganisationId, expectedEndpoint), Times.Once);
    }

    [Test]
    public async Task WhenMethodIsCalledWithCacheAllowed_ThenApiRequestIsSentOnlyOnce()
    {
        var operatorOrganisationId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();

        _accountServiceApiClient
            .Setup(client => client.SendGetRequest(operatorOrganisationId, $"compliance-schemes/{complianceSchemeId}/summary"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ComplianceSchemeSummary().ToJsonContent()
            });

        await _complianceSchemeService.GetComplianceSchemeSummary(operatorOrganisationId, complianceSchemeId);

        // set cache on the first call
        await _complianceSchemeService.GetComplianceSchemeSummary(operatorOrganisationId, complianceSchemeId);
        _accountServiceApiClient.Verify(client => client.SendGetRequest(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);

        _accountServiceApiClient.Invocations.Clear();

        // read from cache on the second call
        await _complianceSchemeService.GetComplianceSchemeSummary(operatorOrganisationId, complianceSchemeId);
        _accountServiceApiClient.Verify(client => client.SendGetRequest(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task WhenApiResponseIsUnsuccessful_ThenThrow()
    {
        _accountServiceApiClient
            .Setup(client => client.SendGetRequest(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.ThrowsAsync<HttpRequestException>(
            () => _complianceSchemeService.GetComplianceSchemeSummary(Guid.NewGuid(), Guid.NewGuid()));
    }
}
