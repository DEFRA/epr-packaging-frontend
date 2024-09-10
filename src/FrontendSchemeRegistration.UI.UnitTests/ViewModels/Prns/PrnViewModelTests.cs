using System.Globalization;
using AutoFixture.NUnit3;
using FluentAssertions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.Extensions.Time.Testing;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns
{
    public class PrnViewModelTests
    {
        [Theory]
        [InlineAutoData("2024-04-07", "2024-07-06")]
        [InlineAutoData("2024-12-01", "2024-12-01")]
        [InlineAutoData("2024-12-01", "2024-12-31")]
        [InlineAutoData("2024-12-01", "2025-01-01")]
        [InlineAutoData("2024-12-01", "2025-01-31")]
        public void DecemberWasteRulesApply_When_IsDecemberWaste_And_SuitableDate_Returns_True(string issueDateString, string currentDateString)
        {
            // Arrange
            var issueDate = DateTime.ParseExact(issueDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var currentDate = DateTime.ParseExact(currentDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var model = new PrnViewModel { DateIssued = issueDate, IsDecemberWaste = true };
            FakeTimeProvider fakeTimeProvider = new FakeTimeProvider();
            fakeTimeProvider.SetUtcNow(currentDate);

            // Act
            var rulesApply = model.DecemberWasteRulesApply(fakeTimeProvider.GetUtcNow().DateTime);

            // Assert
            rulesApply.Should().Be(true);
        }

        [InlineAutoData(false, "2024-12-01", "2024-12-01")]
        [InlineAutoData(true, "2024-12-01", "2023-12-31")]
        [InlineAutoData(true, "2024-12-01", "2025-02-01")]
        public void DecemberWasteRulesApply_When_IsNotDecemberWaste_Or_UnSuitableDate_Returns_False(bool isDecemberWaste, string issueDateString, string currentDateString)
        {
            // Arrange
            var issueDate = DateTime.ParseExact(issueDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var currentDate = DateTime.ParseExact(currentDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var model = new PrnViewModel { DateIssued = issueDate, IsDecemberWaste = isDecemberWaste };
            FakeTimeProvider fakeTimeProvider = new FakeTimeProvider();
            fakeTimeProvider.SetUtcNow(currentDate);

            // Act
            var rulesApply = model.DecemberWasteRulesApply(fakeTimeProvider.GetUtcNow().DateTime);

            // Assert
            rulesApply.Should().Be(false);
        }
    }
}
