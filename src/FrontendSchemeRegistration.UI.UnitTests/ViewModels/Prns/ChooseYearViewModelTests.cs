using FluentAssertions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns;

[TestFixture]
public class ChooseYearViewModelTests
{
    [Test]
    public void Constructor_SetsExpectedDefaults()
    {
        var sut = new ChooseYearViewModel();

        sut.CsocUnavailablePastYear.Should().Be(2025);
        sut.SelectedYear.Should().BeNull();
        sut.CurrentYear.Should().Be(0);
        sut.Years.Should().BeEmpty();
    }

    [Test]
    public void Properties_CanBeSetAndRetrieved()
    {
        var years = new List<int> { 2024, 2025, 2026 };

        var sut = new ChooseYearViewModel
        {
            CsocUnavailablePastYear = 2020,
            SelectedYear = 2025,
            CurrentYear = 2026,
            Years = years
        };

        sut.CsocUnavailablePastYear.Should().Be(2020);
        sut.SelectedYear.Should().Be(2025);
        sut.CurrentYear.Should().Be(2026);
        sut.Years.Should().BeSameAs(years);
    }
}
