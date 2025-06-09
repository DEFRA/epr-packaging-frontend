using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;

namespace FrontendSchemeRegistration.Tests.Options
{
    [TestFixture]
    public class LargeProducerRegistrationWarningTests
    {
        [Test]
        public void IsActiveToday_ReturnsTrue_WhenTodayIsWithinRange()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var model = new LargeProducerRegistrationWarning
            {
                StartDate = today.AddDays(-1).ToString("yyyy/MM/dd"),
                EndDate = today.AddDays(1).ToString("yyyy/MM/dd")
            };

            // Act
            var result = model.IsActiveToday();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsActiveToday_ReturnsFalse_WhenTodayIsBeforeStartDate()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var model = new LargeProducerRegistrationWarning
            {
                StartDate = today.AddDays(1).ToString("yyyy/MM/dd"),
                EndDate = today.AddDays(5).ToString("yyyy/MM/dd")
            };

            // Act
            var result = model.IsActiveToday();

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsActiveToday_ReturnsFalse_WhenTodayIsAfterEndDate()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var model = new LargeProducerRegistrationWarning
            {
                StartDate = today.AddDays(-5).ToString("yyyy/MM/dd"),
                EndDate = today.AddDays(-1).ToString("yyyy/MM/dd")
            };

            // Act
            var result = model.IsActiveToday();

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsActiveToday_ReturnsFalse_WhenDateParsingFails()
        {
            // Arrange
            var model = new LargeProducerRegistrationWarning
            {
                StartDate = "invalid-start-date",
                EndDate = "invalid-end-date"
            };

            // Act
            var result = model.IsActiveToday();

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsActiveToday_ReturnsTrue_WhenTodayEqualsStartAndEndDate()
        {
            // Arrange
            var today = DateTime.UtcNow.Date.ToString("yyyy/MM/dd");
            var model = new LargeProducerRegistrationWarning
            {
                StartDate = today,
                EndDate = today
            };

            // Act
            var result = model.IsActiveToday();

            // Assert
            result.Should().BeTrue();
        }
    }
}
