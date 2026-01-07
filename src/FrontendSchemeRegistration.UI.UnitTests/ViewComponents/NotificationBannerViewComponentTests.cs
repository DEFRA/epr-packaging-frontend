using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents;

using Microsoft.Extensions.Time.Testing;
using UI.ViewModels.Shared.Components.NotificationBanner;

public class NotificationBannerViewComponentTests
{
    private Mock<IFeatureManagerSnapshot> _featureManagerMock;
    private Mock<IOptionsSnapshot<NotificationBannerOptions>> _featureOptionsMock;
    private FakeTimeProvider _fakeTimeProvider;
    private NotificationBannerViewComponent _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _featureManagerMock = new Mock<IFeatureManagerSnapshot>();
        _featureOptionsMock = new Mock<IOptionsSnapshot<NotificationBannerOptions>>();
        _fakeTimeProvider = new FakeTimeProvider();
        
        _systemUnderTest = new NotificationBannerViewComponent(
            _featureManagerMock.Object,
            _featureOptionsMock.Object,
            _fakeTimeProvider);
    }

    [Test]
    public async Task InvokeAsync_WhenFeatureFlagDisabled_ReturnsEmptyContent()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ContentViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().BeEmpty();
        _featureManagerMock.Verify(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenFeatureFlagEnabledButNoActiveNotifications_ReturnsEmptyContent()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications = []
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ContentViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().BeEmpty();
    }

    [Test]
    public async Task InvokeAsync_WhenFeatureFlagEnabledAndActiveNotificationExists_ReturnsView()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "test_notification",
                    EffectiveFrom = now.AddDays(-1),
                    EffectiveTo = now.AddDays(1)
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ViewViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.Should().NotBeNull();
        result!.ViewData!.Model.Should().BeOfType<NotificationBannerViewModel>();
        
        var model = result.ViewData.Model as NotificationBannerViewModel;
        model!.TitleResource.Should().Be("test_notification_title");
        model.HeaderResource.Should().Be("test_notification_header");
        model.BodyResource.Should().Be("test_notification_body");
    }

    [Test]
    public async Task InvokeAsync_WhenNotificationOutsideDateRange_ReturnsEmptyContent()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "expired_notification",
                    EffectiveFrom = now.AddDays(-10),
                    EffectiveTo = now.AddDays(-5)
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ContentViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().BeEmpty();
    }

    [Test]
    public async Task InvokeAsync_WhenNotificationNotYetEffective_ReturnsEmptyContent()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "future_notification",
                    EffectiveFrom = now.AddDays(5),
                    EffectiveTo = now.AddDays(10)
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ContentViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().BeEmpty();
    }

    [Test]
    public async Task InvokeAsync_WhenMultipleOverlappingNotifications_ReturnsNotificationWithLatestEffectiveFrom()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "older_notification",
                    EffectiveFrom = now.AddDays(-5),
                    EffectiveTo = now.AddDays(5)
                },
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "newer_notification",
                    EffectiveFrom = now.AddDays(-2),
                    EffectiveTo = now.AddDays(2)
                },
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "newest_notification",
                    EffectiveFrom = now.AddDays(-1),
                    EffectiveTo = now.AddDays(1)
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ViewViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.Should().NotBeNull();
        result!.ViewData!.Model.Should().BeOfType<NotificationBannerViewModel>();
        
        var model = result.ViewData.Model as NotificationBannerViewModel;
        model!.TitleResource.Should().Be("newest_notification_title");
        model.HeaderResource.Should().Be("newest_notification_header");
        model.BodyResource.Should().Be("newest_notification_body");
    }

    [Test]
    public async Task InvokeAsync_WhenNotificationAtExactStartTime_ReturnsView()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "boundary_notification",
                    EffectiveFrom = now,
                    EffectiveTo = now.AddDays(1)
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ViewViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.Should().NotBeNull();
        result!.ViewData!.Model.Should().BeOfType<NotificationBannerViewModel>();
        
        var model = result.ViewData.Model as NotificationBannerViewModel;
        model!.TitleResource.Should().Be("boundary_notification_title");
    }

    [Test]
    public async Task InvokeAsync_WhenNotificationAtExactEndTime_ReturnsView()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "boundary_notification",
                    EffectiveFrom = now.AddDays(-1),
                    EffectiveTo = now
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ViewViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.Should().NotBeNull();
        result!.ViewData!.Model.Should().BeOfType<NotificationBannerViewModel>();
        
        var model = result.ViewData.Model as NotificationBannerViewModel;
        model!.TitleResource.Should().Be("boundary_notification_title");
    }

    [Test]
    public async Task InvokeAsync_WhenMultipleNonOverlappingNotifications_ReturnsOnlyActiveOne()
    {
        // Arrange
        var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new NotificationBannerOptions
        {
            Notifications =
            [
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "expired_notification",
                    EffectiveFrom = now.AddDays(-10),
                    EffectiveTo = now.AddDays(-5)
                },
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "active_notification",
                    EffectiveFrom = now.AddDays(-1),
                    EffectiveTo = now.AddDays(1)
                },
                new NotificationBannerOptions.NotificationOption
                {
                    ResourceKey = "future_notification",
                    EffectiveFrom = now.AddDays(5),
                    EffectiveTo = now.AddDays(10)
                }
            ]
        };

        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(FeatureFlags.ShowNotificationBanner))
            .ReturnsAsync(true);
        _featureOptionsMock.Setup(x => x.Value).Returns(options);
        _fakeTimeProvider.SetUtcNow(now);

        // Act
        var result = await _systemUnderTest.InvokeAsync() as ViewViewComponentResult;

        // Assert
        result.Should().NotBeNull();
        result!.ViewData.Should().NotBeNull();
        result!.ViewData!.Model.Should().BeOfType<NotificationBannerViewModel>();
        
        var model = result.ViewData.Model as NotificationBannerViewModel;
        model!.TitleResource.Should().Be("active_notification_title");
        model.HeaderResource.Should().Be("active_notification_header");
        model.BodyResource.Should().Be("active_notification_body");
    }
}