using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options.ReistrationPeriodPatterns;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FrontendSchemeRegistration.UI.UnitTests.Services.RegistrationPeriods;

[TestFixture]
public class RegistrationPeriodProviderTests
{
    private FakeTimeProvider _timeProvider;

    [SetUp]
    public void Setup()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void WHEN_constructor_called_with_valid_config_THEN_parsePatterns_creates_registration_windows()
    {
        // arrange
        var registrationPeriodPatterns = CreateValidRegistrationPatterns();
        var options = Options.Create(registrationPeriodPatterns);

        // act
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var windows = sut.GetRegistrationWindows(isCso: true);

        // assert
        windows.Should().NotBeEmpty();
        windows.Should().AllSatisfy(w => w.RegistrationYear.Should().BeGreaterOrEqualTo(2026));
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_for_CSO_THEN_returns_only_CSO_journeys_and_null_journeys()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window>
                {
                    CreateWindow(WindowType.CsoLargeProducer),
                    CreateWindow(WindowType.DirectLargeProducer),
                    CreateWindow(WindowType.Cso) // null journey
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetRegistrationWindows(isCso: true);

        // assert
        windows.Should().NotBeEmpty();
        windows.Should().AllSatisfy(w =>
        {
            var isValid = w.Journey == RegistrationJourney.CsoLargeProducer ||
                          w.Journey == RegistrationJourney.CsoSmallProducer ||
                          w.Journey == null;
            isValid.Should().BeTrue();
        });
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_for_non_CSO_THEN_returns_only_Direct_journeys_and_null_journeys()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window>
                {
                    CreateWindow(WindowType.DirectLargeProducer),
                    CreateWindow(WindowType.CsoLargeProducer),
                    CreateWindow(WindowType.Direct) // null journey
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetRegistrationWindows(isCso: false);

        // assert
        windows.Should().NotBeEmpty();
        windows.Should().AllSatisfy(w =>
        {
            var isValid = w.Journey == RegistrationJourney.DirectLargeProducer ||
                          w.Journey == RegistrationJourney.DirectSmallProducer ||
                          w.Journey == null;
            isValid.Should().BeTrue();
        });
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_THEN_windows_are_ordered_by_registration_year_descending()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2024,
                FinalRegistrationYear = 2026,
                Windows = new List<Window>
                {
                    CreateWindow(WindowType.CsoLargeProducer)
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetRegistrationWindows(isCso: true).ToList();

        // assert
        windows.Should().BeInDescendingOrder(w => w.RegistrationYear);
    }

    [Test]
    public void WHEN_constructor_called_with_duplicate_registration_years_THEN_throws_InvalidOperationException()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window> { CreateWindow(WindowType.CsoLargeProducer) }
            },
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window> { CreateWindow(WindowType.DirectLargeProducer) }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationPeriodProvider(options, _timeProvider));
        ex.Message.Should().Contain("configured in multiple RegistrationPeriodPattern");
    }

    [Test]
    public void WHEN_constructor_called_THEN_excludes_closed_registration_windows()
    {
        // arrange
        // Create a window that closed in the past
        var closedWindow = CreateWindow(WindowType.CsoLargeProducer, 
            openingDateOffset: new DateTime(2025, 1, 1), 
            deadlineDateOffset: new DateTime(2025, 2, 1),
            closingDateOffset: new DateTime(2025, 3, 1));

        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2025,
                FinalRegistrationYear = 2025,
                Windows = new List<Window> { closedWindow }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);

        // act
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var windows = sut.GetRegistrationWindows(isCso: true);

        // assert
        windows.Should().BeEmpty();
    }

    [Test]
    public void WHEN_constructor_called_with_null_final_registration_year_THEN_uses_current_year()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2025,
                FinalRegistrationYear = null,  // No final year specified, should use current year
                Windows = new List<Window> { CreateWindow(WindowType.CsoLargeProducer) }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);

        // act
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var windows = sut.GetRegistrationWindows(isCso: true);

        // assert
        windows.Should().Contain(w => w.RegistrationYear == 2026); // 2026 is the current year in _timeProvider
        windows.Should().NotContain(w => w.RegistrationYear > 2026);
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_with_multiple_window_types_THEN_maps_correctly()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window>
                {
                    CreateWindow(WindowType.CsoLargeProducer),
                    CreateWindow(WindowType.CsoSmallProducer),
                    CreateWindow(WindowType.DirectLargeProducer),
                    CreateWindow(WindowType.DirectSmallProducer)
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var csoWindows = sut.GetRegistrationWindows(isCso: true);
        var directWindows = sut.GetRegistrationWindows(isCso: false);

        // assert
        csoWindows.Should().HaveCount(2);
        directWindows.Should().HaveCount(2);
        csoWindows.Should().Contain(w => w.Journey == RegistrationJourney.CsoLargeProducer);
        csoWindows.Should().Contain(w => w.Journey == RegistrationJourney.CsoSmallProducer);
        directWindows.Should().Contain(w => w.Journey == RegistrationJourney.DirectLargeProducer);
        directWindows.Should().Contain(w => w.Journey == RegistrationJourney.DirectSmallProducer);
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_with_null_journey_windows_THEN_includes_for_both_CSO_and_non_CSO()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window>
                {
                    CreateWindow(WindowType.Cso),      // null journey
                    CreateWindow(WindowType.Direct)    // null journey
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var csoWindows = sut.GetRegistrationWindows(isCso: true);
        var directWindows = sut.GetRegistrationWindows(isCso: false);

        // assert
        csoWindows.Should().HaveCount(2);
        directWindows.Should().HaveCount(2);
        csoWindows.Should().AllSatisfy(w => w.Journey.Should().BeNull());
        directWindows.Should().AllSatisfy(w => w.Journey.Should().BeNull());
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_THEN_returns_read_only_collection()
    {
        // arrange
        var registrationPeriodPatterns = CreateValidRegistrationPatterns();
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetRegistrationWindows(isCso: true);

        // assert
        windows.Should().BeAssignableTo<IReadOnlyCollection<RegistrationWindow>>();
    }

    // Helper methods
    private static List<RegistrationPeriodPattern> CreateValidRegistrationPatterns()
    {
        return new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window>
                {
                    CreateWindow(WindowType.CsoLargeProducer),
                    CreateWindow(WindowType.CsoSmallProducer),
                    CreateWindow(WindowType.DirectLargeProducer),
                    CreateWindow(WindowType.DirectSmallProducer)
                }
            }
        };
    }

    private static Window CreateWindow(
        WindowType windowType,
        DateTime? openingDateOffset = null,
        DateTime? deadlineDateOffset = null,
        DateTime? closingDateOffset = null)
    {
        var opening = openingDateOffset ?? new DateTime(2026, 6, 1);
        var deadline = deadlineDateOffset ?? new DateTime(2026, 7, 1);
        var closing = closingDateOffset ?? new DateTime(2026, 8, 1);

        return new Window
        {
            WindowType = windowType,
            OpeningDate = new Date { Day = opening.Day, Month = opening.Month, YearOffset = opening.Year - 2026 },
            DeadlineDate = new Date { Day = deadline.Day, Month = deadline.Month, YearOffset = deadline.Year - 2026 },
            ClosingDate = new Date { Day = closing.Day, Month = closing.Month, YearOffset = closing.Year - 2026 }
        };
    }
}