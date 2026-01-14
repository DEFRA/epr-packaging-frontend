namespace FrontendSchemeRegistration.Application.UnitTests.Helpers;

using FluentAssertions;
using FrontendSchemeRegistration.Application.Helpers;

[TestFixture]
public class TimeTravelTestingTimeProviderTests
{
    private TimeTravelTestingTimeProvider _timeProvider = null!;

    [Test]
    public void GIVEN_simulating_the_past_WHEN_GetUtcNow_called_THEN_offset_is_correct()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddDays(-5);

        // Act
        _timeProvider = new TimeTravelTestingTimeProvider(pastDateTime);

        // Assert
        var result = _timeProvider.GetUtcNow();
        result.Should().BeCloseTo(pastDateTime, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void GIVEN_simulating_the_past_WHEN_GetLocalNow_called_THEN_offset_is_correct()
    {
        // Arrange
        var pastDateTime = DateTime.UtcNow.AddDays(-5);

        // Act
        _timeProvider = new TimeTravelTestingTimeProvider(pastDateTime);

        // Assert
        var result = _timeProvider.GetLocalNow();
        result.Should().BeCloseTo(pastDateTime, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void GIVEN_simulating_the_future_WHEN_GetUtcNow_called_THEN_offset_is_correct()
    {
        // Arrange
        var futureDateTime = DateTime.UtcNow.AddDays(10);

        // Act
        _timeProvider = new TimeTravelTestingTimeProvider(futureDateTime);

        // Assert
        var result = _timeProvider.GetUtcNow();
        result.Should().BeCloseTo(futureDateTime, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void GIVEN_simulating_the_present_WHEN_GetUtcNow_called_THEN_returns_current_time()
    {
        // Arrange
        var currentDateTime = DateTime.UtcNow;

        // Act
        _timeProvider = new TimeTravelTestingTimeProvider(currentDateTime);

        // Assert
        var result = _timeProvider.GetUtcNow();
        result.Should().BeCloseTo(currentDateTime, TimeSpan.FromSeconds(10));
    }

    [Test]
    public void GIVEN_simulating_year_2000_WHEN_GetUtcNow_called_THEN_returns_correct_offset()
    {
        // Arrange
        var veryOldDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _timeProvider = new TimeTravelTestingTimeProvider(veryOldDate);

        // Act
        var result = _timeProvider.GetUtcNow();

        // Assert
        result.DateTime.Should().BeCloseTo(veryOldDate, TimeSpan.FromSeconds(10));
    }

    [Test]
    public Task GIVEN_simulating_the_past_WHEN_GetUtcNow_called_multiple_times_THEN_returns_consistent_results()
    {
        // Arrange
        var targetDateTime = DateTime.UtcNow.AddHours(-3);
        _timeProvider = new TimeTravelTestingTimeProvider(targetDateTime);

        // Act
        var firstCall = _timeProvider.GetUtcNow();
        Task.Delay(100);
        var secondCall = _timeProvider.GetUtcNow();

        // Assert
        firstCall.Should().BeCloseTo(secondCall, TimeSpan.FromMilliseconds(200));
        firstCall.DateTime.Should().BeCloseTo(targetDateTime, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }
}
