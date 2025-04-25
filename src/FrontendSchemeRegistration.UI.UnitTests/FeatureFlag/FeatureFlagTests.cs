using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FrontendSchemeRegistration.UI.UnitTests.FeatureFlag;

[TestFixture]
public class FeatureFlagTests
{
    private IConfiguration configuration;

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ShowYourFeedbackFooter_Should_Be_True(bool value)
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>
        {
            {FrontendSchemeRegistration.UI.Constants.FeatureFlags.ShowYourFeedbackFooter, value.ToString()}
        };

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        bool showYourFeedbackFooter = bool.Parse(configuration[FrontendSchemeRegistration.UI.Constants.FeatureFlags.ShowYourFeedbackFooter]);

        // Assert
        showYourFeedbackFooter.Should().Be(value);
    }
}
