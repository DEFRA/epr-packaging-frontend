namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Localization;
using Moq;
using UI.Extensions;

[TestFixture]
public class ViewLocalizerExtensionsTests
{
    private Mock<IViewLocalizer> _viewLocalizerMock;

    [SetUp]
    public void SetUp()
    {
        _viewLocalizerMock = new Mock<IViewLocalizer>();
    }

    [Test]
    public void DecemberWasteFlashText_ShouldReturnFormattedSingleYearText_WhenOneAcceptanceYearAvailable()
    {
        // Arrange
        _viewLocalizerMock
            .Setup(x => x["acceptance_year"])
            .Returns(new LocalizedHtmlString("acceptance_year", "December {0} waste"));

        // Act
        var result = _viewLocalizerMock.Object.DecemberWasteFlashText(new[] { 2024 });

        // Assert
        result.Should().Be("December 2024 waste");
    }

    [Test]
    public void DecemberWasteFlashText_ShouldReturnFormattedTwoYearsText_WhenTwoAcceptanceYearsAvailable()
    {
        // Arrange
        _viewLocalizerMock
            .Setup(x => x["acceptance_years"])
            .Returns(new LocalizedHtmlString("acceptance_years", "December {0} or {1} waste"));

        // Act
        var result = _viewLocalizerMock.Object.DecemberWasteFlashText(new[] { 2024, 2025 });

        // Assert
        result.Should().Be("December 2024 or 2025 waste");
    }

    [Test]
    public void DecemberWasteFlashText_ShouldReturnEmptyString_WhenNoAcceptanceYearsAvailable()
    {
        // Act
        var result = _viewLocalizerMock.Object.DecemberWasteFlashText(Array.Empty<int>());

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void DecemberWasteFlashText_ShouldReturnEmptyString_WhenMoreThanTwoAcceptanceYearsAvailable()
    {
        // Act
        var result = _viewLocalizerMock.Object.DecemberWasteFlashText(new[] { 2023, 2024, 2025 });

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void DecemberWasteFlashText_ShouldUseOnlyFirstAcceptanceYear_WhenOneAcceptanceYearAvailable()
    {
        // Arrange
        _viewLocalizerMock
            .Setup(x => x["acceptance_year"])
            .Returns(new LocalizedHtmlString("acceptance_year", "Year: {0}"));

        // Act
        var result = _viewLocalizerMock.Object.DecemberWasteFlashText(new[] { 2030 });

        // Assert
        result.Should().Be("Year: 2030");
        _viewLocalizerMock.Verify(x => x["acceptance_years"], Times.Never);
    }
}
