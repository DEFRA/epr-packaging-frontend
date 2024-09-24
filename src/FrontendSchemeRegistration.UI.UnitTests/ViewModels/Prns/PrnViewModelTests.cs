using System.Globalization;
using FluentAssertions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.Extensions.Time.Testing;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns
{
    public class PrnViewModelTests
    {
        [TestCase("2024-04-07", "2024-07-06")]
        [TestCase("2024-12-01", "2024-12-01")]
        [TestCase("2024-12-01", "2024-12-31")]
        [TestCase("2024-12-01", "2025-01-01")]
        [TestCase("2024-12-01", "2025-01-31")]
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

        [TestCase(false, "2024-12-01", "2024-12-01")]
        [TestCase(true, "2024-12-01", "2023-12-31")]
        [TestCase(true, "2024-12-01", "2025-02-01")]
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
