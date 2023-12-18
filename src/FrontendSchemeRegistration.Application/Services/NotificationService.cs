namespace FrontendSchemeRegistration.Application.Services;

using System.Net;
using DTOs.Notification;
using Extensions;
using Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Options;

public class NotificationService : INotificationService
{
    private const string GetNotificationErrorMessage = "Attempting to get notifications failed";
    private const string _getNotificationPath = "notifications?serviceKey=Packaging";
    private readonly ILogger<NotificationService> _logger;
    private readonly IAccountServiceApiClient _accountServiceApiClient;
    private readonly IDistributedCache _cache;
    private readonly CachingOptions _cachingOptions;

    public NotificationService(
        IAccountServiceApiClient accountServiceApiClient,
        ILogger<NotificationService> logger,
        IDistributedCache cache,
        IOptions<CachingOptions> cachingOptions)
    {
        _logger = logger;
        _accountServiceApiClient = accountServiceApiClient;
        _cache = cache;
        _cachingOptions = cachingOptions.Value;
    }

    /// <summary>Gets notifications for current user for an organisation.</summary>
    /// <returns>NotificationsDto.</returns>
    /// <param name="organisationId">Organisation Identifier.</param>
    /// /// <param name="userId">User Identifier (used only for caching key).</param>
    public async Task<List<NotificationDto>?> GetCurrentUserNotifications(Guid organisationId, Guid userId)
    {
        var notificationsResponseCacheKey = GetCacheKey(organisationId, userId);

        try
        {
            if (_cachingOptions.CacheNotifications && _cache.TryGetValue(notificationsResponseCacheKey, out NotificationsResponse notificationsResponse))
            {
                if (notificationsResponse?.Notifications?.Any() != true)
                {
                    return null;
                }
            }
            else
            {
                _accountServiceApiClient.AddHttpClientHeader("X-EPR-Organisation", organisationId.ToString());
                var result = await _accountServiceApiClient.SendGetRequest(_getNotificationPath);
                _accountServiceApiClient.RemoveHttpClientHeader("X-EPR-Organisation");

                var cacheEntryOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(_cachingOptions.SlidingExpirationSeconds))
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cachingOptions.AbsoluteExpirationSeconds));

                if (result.StatusCode == HttpStatusCode.NoContent)
                {
                    await _cache.SetAsync<NotificationsResponse>(notificationsResponseCacheKey, new NotificationsResponse { Notifications = new List<NotificationDto>() }, cacheEntryOptions);
                    return null;
                }

                var content = await result.Content.ReadAsStringAsync();
                notificationsResponse = JsonConvert.DeserializeObject<NotificationsResponse>(content);

                if (notificationsResponse == null)
                {
                    await _cache.SetAsync<NotificationsResponse>(notificationsResponseCacheKey, new NotificationsResponse { Notifications = new List<NotificationDto>() }, cacheEntryOptions);
                    return null;
                }

                await _cache.SetAsync<NotificationsResponse>(notificationsResponseCacheKey, notificationsResponse, cacheEntryOptions);
            }

            return notificationsResponse.Notifications.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, GetNotificationErrorMessage);
            throw;
        }
    }

    public async Task ResetCache(Guid organisationId, Guid userId)
    {
        var cacheKey = GetCacheKey(organisationId, userId);

        await _cache.RemoveAsync(cacheKey);
    }

    private static string GetCacheKey(Guid organisationId, Guid userId)
    {
        return $"Notifications_{organisationId}_{userId}";
    }
}