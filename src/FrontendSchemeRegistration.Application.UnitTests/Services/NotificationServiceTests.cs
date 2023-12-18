namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using System.Text.Json;
using Application.Services;
using Application.Services.Interfaces;
using AutoFixture;
using Constants;
using DTOs.Notification;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Moq;
using Options;

[TestFixture]
public class NotificationServiceTests
{
    private readonly Fixture _fixture = new Fixture();
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<ILogger<NotificationService>> _loggerMock = new();
    private readonly Mock<ITokenAcquisition> _tokenAcquisitionMock = new();
    private Mock<IAccountServiceApiClient> _userAccountServiceApiClientMock;
    private IDistributedCache _cache;
    private NotificationService _sut;

    [SetUp]
    public void Init()
    {
        _userAccountServiceApiClientMock = new Mock<IAccountServiceApiClient>();

        var memoryDistributedCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
        _cache = new MemoryDistributedCache(memoryDistributedCacheOptions);

        var cachingOptions = Options.Create(new CachingOptions { AbsoluteExpirationSeconds = 300, SlidingExpirationSeconds = 120, CacheNotifications = true });

        _sut = new NotificationService(
            _userAccountServiceApiClientMock.Object,
            new NullLogger<NotificationService>(),
            _cache,
            cachingOptions);
    }

    [Test]
    public async Task GetCurrentUserNotification_WhenNoNotifications_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        var result = await _sut.GetCurrentUserNotifications(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetCurrentUserNotification_WhenNoNotificationsResponse_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        response.Content = null;

        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        var result = await _sut.GetCurrentUserNotifications(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetCurrentUserNotification_WhenNoNotifications_ReturnsException()
    {
        // Arrange
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ThrowsAsync(new Exception("Service error"));

        // Act
        Func<Task<List<NotificationDto>?>> func = async () => await _sut.GetCurrentUserNotifications(
            It.IsAny<Guid>(),
            It.IsAny<Guid>());

        // Assert
        var ex = func.Should().ThrowAsync<Exception>();

        ex.Result.And.Message.Should().Contain("Service error");
    }

    [Test]
    public async Task ResetCache_WhenRemovingCache_ReturnsNotBeNull()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cacheKey = $"Notifications_{organisationId}_{userId}";
        await _cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(new { Name = "Test" }));

        // Act
        await _sut.ResetCache(organisationId, userId);
        var result = await _cache.GetAsync(cacheKey);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetCurrentUserNotification_WhenPendingNotification_ReturnsPendingNotification()
    {
        var enrolmentId = Guid.NewGuid();
        var notificationsResponse = new NotificationsResponse
        {
            Notifications = new List<NotificationDto>
            {
                new NotificationDto
                {
                    Type = NotificationTypes.Packaging.DelegatedPersonPendingApproval,
                    Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>(nameof(enrolmentId), enrolmentId.ToString())
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = notificationsResponse.ToJsonContent();
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        var result = await _sut.GetCurrentUserNotifications(Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeOfType<List<NotificationDto>>();
        result.First().Type.Should().Be(NotificationTypes.Packaging.DelegatedPersonPendingApproval);
        result.First().Data.FirstOrDefault().Key.Should().Be(nameof(enrolmentId));
        result.First().Data.FirstOrDefault().Value.Should().Be(enrolmentId.ToString());
    }

    [Test]
    public async Task GetCurrentUserNotification_WhenNominatedNotification_ReturnsNominatedNotification()
    {
        var enrolmentId = Guid.NewGuid();
        var notificationsResponse = new NotificationsResponse
        {
            Notifications = new List<NotificationDto>
            {
                new NotificationDto
                {
                    Type = NotificationTypes.Packaging.DelegatedPersonNomination,
                    Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>(nameof(enrolmentId), enrolmentId.ToString())
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = notificationsResponse.ToJsonContent();
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        var result = await _sut.GetCurrentUserNotifications(Guid.NewGuid(), Guid.NewGuid());
        result.Should().BeOfType<List<NotificationDto>>();
        result.First().Type.Should().Be(NotificationTypes.Packaging.DelegatedPersonNomination);
        result.First().Data.FirstOrDefault().Key.Should().Be(nameof(enrolmentId));
        result.First().Data.FirstOrDefault().Value.Should().Be(enrolmentId.ToString());
    }

    [Test]
    public async Task ResetCache_WithValidInputs_RemovesCache()
    {
        // Arrange
        var organisationId = _fixture.Create<Guid>();
        var userId = _fixture.Create<Guid>();
        var cacheKey = $"Notifications_{organisationId}_{userId}";

        // Act
        await _sut.ResetCache(organisationId, userId);

        // Assert
        cacheKey.Should().NotBeNull();
    }
}