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
    private FakeTimeProvider _timeProvider;     // starting time for each test is 2026-01-15:12:00:00
    private const int RegistrationYear = 2026; 

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
        var windows = sut.GetActiveRegistrationWindows(isCso: true);

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
        var windows = sut.GetActiveRegistrationWindows(isCso: true);

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
        var windows = sut.GetActiveRegistrationWindows(isCso: false);

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
        var windows = sut.GetActiveRegistrationWindows(isCso: true).ToList();

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
    public void WHEN_constructor_called_THEN_includes_closed_registration_windows()
    {
        // arrange
        // Create a window that closed in the past
            var closedWindow = CreateWindow(WindowType.CsoLargeProducer,
            openingDateConfig: new Date { Day = 1, Month = 1 },
            deadlineDateConfig: new Date { Day = 1, Month = 2 },
            closingDateConfig: new Date { Day = 1, Month = 3 });

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
        var windows = sut.GetActiveRegistrationWindows(isCso: true);

        // assert
        windows.Should().NotBeEmpty();
    }

    [Test]
    public void GIVEN_initially_open_window_closes_WHEN_GetRegistrationWindows_called_THEN_includes_closed_registration_windows()
    {
        // arrange
        // Create a window that will close
        var closingWindow = CreateWindow(WindowType.CsoLargeProducer,
            openingDateConfig: new Date { Day = 1, Month = 1 },
            deadlineDateConfig: new Date { Day = 1, Month = 2 },
            closingDateConfig: new Date { Day = 1, Month = 3 });

        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window> { closingWindow, CreateWindow(WindowType.CsoSmallProducer) }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var initialWindows = sut.GetActiveRegistrationWindows(isCso: true);

        // act
        // move past close date
        _timeProvider.SetUtcNow(new DateTime(2026, 4, 1));
        var finalWindows = sut.GetActiveRegistrationWindows(isCso: true);

        // assert
        initialWindows.Count.Should().Be(2);
        finalWindows.Count.Should().Be(2);
    }

    [Test]
    public void GIVEN_null_final_registration_year_WHEN_year_rolls_over_THEN_GetRegistrationWindows_returns_windows_for_new_year()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = null,  // No final year specified, should use current year
                Windows = new List<Window> { CreateWindow(WindowType.CsoLargeProducer) }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var initialWindows = sut.GetActiveRegistrationWindows(isCso: true);

        // act
        _timeProvider.SetUtcNow(new DateTime(2027, 1, 1));
        var finalWindows = sut.GetActiveRegistrationWindows(isCso: true);

        // assert
        initialWindows.Should().Contain(w => w.RegistrationYear == 2026);
        initialWindows.Should().NotContain(w => w.RegistrationYear > 2026);
        finalWindows.Should().Contain(w => w.RegistrationYear == 2027);
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
        var windows = sut.GetActiveRegistrationWindows(isCso: true);

        // assert
        windows.Should().Contain(w => w.RegistrationYear == 2026); // 2026 is the current year in _timeProvider
        windows.Should().NotContain(w => w.RegistrationYear > 2026);
    }

    [Test]
    public void WHEN_constructor_called_with_null_final_registration_year_and_offset_opening_date_THEN_uses_current_year()
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            // 2025 should all be closed, but still returned
            // 2026 should all be returned
            // 2027 should be returned because the opening date is in 2026
            // 2028 should not be returned
            new()
            {
                InitialRegistrationYear = 2025,
                FinalRegistrationYear = null,  // No final year specified
                Windows = new List<Window>
                {
                    CreateWindow(
                        WindowType.CsoLargeProducer,
                        new Date { Day = 1, Month = 1, YearOffset = -1},
                        new Date { Day = 1, Month = 7 },
                        new Date { Day = 1, Month = 8 })
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);

        // act
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var windows = sut.GetActiveRegistrationWindows(isCso: true);

        // assert
        windows.Should().Contain(w => w.RegistrationYear == 2025);
        windows.Should().Contain(w => w.RegistrationYear == 2026);
        windows.Should().Contain(w => w.RegistrationYear == 2027);
        windows.Should().NotContain(w => w.RegistrationYear > 2027);
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
        var csoWindows = sut.GetActiveRegistrationWindows(isCso: true);
        var directWindows = sut.GetActiveRegistrationWindows(isCso: false);

        // assert
        csoWindows.Should().HaveCount(2);
        directWindows.Should().HaveCount(2);
        csoWindows.Should().Contain(w => w.Journey == RegistrationJourney.CsoLargeProducer);
        csoWindows.Should().Contain(w => w.Journey == RegistrationJourney.CsoSmallProducer);
        directWindows.Should().Contain(w => w.Journey == RegistrationJourney.DirectLargeProducer);
        directWindows.Should().Contain(w => w.Journey == RegistrationJourney.DirectSmallProducer);
    }

    [Test]
    public void GIVEN_config_windows_which_generate_null_registration_journey_WHEN_GetRegistrationWindows_called_THEN_correct_windows_returned()
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
                    CreateWindow(WindowType.Cso),      // null registration journey
                    CreateWindow(WindowType.Direct)    // null registration journey
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var csoWindows = sut.GetActiveRegistrationWindows(isCso: true);
        var directWindows = sut.GetActiveRegistrationWindows(isCso: false);

        // assert
        csoWindows.Should().HaveCount(1);
        directWindows.Should().HaveCount(1);
        csoWindows.Should().AllSatisfy(w => w.Journey.Should().BeNull());
        csoWindows.Should().AllSatisfy(w => w.IsCso.Should().BeTrue());
        directWindows.Should().AllSatisfy(w => w.Journey.Should().BeNull());
        directWindows.Should().AllSatisfy(w => w.IsCso.Should().BeFalse());
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_THEN_returns_read_only_collection()
    {
        // arrange
        var registrationPeriodPatterns = CreateValidRegistrationPatterns();
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetActiveRegistrationWindows(isCso: true);

        // assert
        windows.Should().BeAssignableTo<IReadOnlyCollection<RegistrationWindow>>();
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_with_multiple_years_THEN_orders_by_registration_year_descending()
    {
        // arrange
        // create windows that stay open for multiple years so that many are active at once
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2024,
                FinalRegistrationYear = 2027,
                Windows = new List<Window>
                {
                    CreateWindow(
                        WindowType.CsoLargeProducer,
                        openingDateConfig: new Date { Day = 15, Month = 2 },
                        deadlineDateConfig: new Date { Day = 1, Month = 3 },
                        closingDateConfig: new Date { Day = 1, Month = 4, YearOffset = 2})
                }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetActiveRegistrationWindows(isCso: true).ToList();

        // assert
        windows.Should().HaveCountGreaterThan(1);
        windows.Should().BeInDescendingOrder(w => w.RegistrationYear);
    }

    [Test]
    public void WHEN_GetRegistrationWindows_called_THEN_filters_out_prior_to_opening_windows()
    {
        // arrange
        // Create windows with opening dates in the future (before current time)
        var futureWindow = CreateWindow(
            WindowType.CsoLargeProducer,
            openingDateConfig: new Date { Day = 15, Month = 2 },  // Opens Feb 15, 2026 (future from Jan 15)
            deadlineDateConfig: new Date { Day = 1, Month = 3 },
            closingDateConfig: new Date { Day = 1, Month = 4 });

        var currentWindow = CreateWindow(
            WindowType.CsoSmallProducer,
            openingDateConfig: new Date { Day = 1, Month = 1 },   // Opens Jan 1, 2026 (past)
            deadlineDateConfig: new Date { Day = 1, Month = 7 },
            closingDateConfig: new Date { Day = 1, Month = 8 });

        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window> { futureWindow, currentWindow }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);
        var sut = new RegistrationPeriodProvider(options, _timeProvider);

        // act
        var windows = sut.GetActiveRegistrationWindows(isCso: true).ToList();

        // assert
        // Future window should be filtered out, only current/past window should remain
        windows.Should().NotContain(w => w.Journey == RegistrationJourney.CsoLargeProducer);
        windows.Should().Contain(w => w.Journey == RegistrationJourney.CsoSmallProducer);
    }

    [TestCase(WindowType.CsoLargeProducer, true)]
    [TestCase(WindowType.CsoSmallProducer, true)]
    [TestCase(WindowType.DirectLargeProducer, false)]
    [TestCase(WindowType.DirectSmallProducer, false)]
    public void WHEN_RegistrationWindowConstructed_with_journey_THEN_IsCsoParameterIsCorrect(WindowType windowType, bool expectedIsCso)
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window> { CreateWindow(windowType) }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);

        // act
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var windows = sut.GetActiveRegistrationWindows(isCso: expectedIsCso).ToList();

        // assert
        windows.Should().HaveCount(1);
        windows[0].IsCso.Should().Be(expectedIsCso);
    }

    [TestCase(WindowType.Cso, true)]
    [TestCase(WindowType.Direct, false)]
    public void WHEN_RegistrationWindowConstructed_with_null_journey_THEN_IsCsoParameterIsCorrect(WindowType windowType, bool expectedIsCso)
    {
        // arrange
        var registrationPeriodPatterns = new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = 2026,
                FinalRegistrationYear = 2026,
                Windows = new List<Window> { CreateWindow(windowType) }
            }
        };
        var options = Options.Create(registrationPeriodPatterns);

        // act
        var sut = new RegistrationPeriodProvider(options, _timeProvider);
        var windows = sut.GetActiveRegistrationWindows(isCso: expectedIsCso).ToList();

        // assert
        windows.Should().HaveCount(1);
        windows[0].IsCso.Should().Be(expectedIsCso);
        windows[0].Journey.Should().BeNull();
    }

    // Creates registration period configuration for 2026 and a number of windows for that period. The windows
    // all have an opening date of 2026-06-01, a deadline date of 2026-07-01 and a closing date of 2026-08-01.
    private static List<RegistrationPeriodPattern> CreateValidRegistrationPatterns()
    {
        return new List<RegistrationPeriodPattern>
        {
            new()
            {
                InitialRegistrationYear = RegistrationYear,
                FinalRegistrationYear = RegistrationYear,
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

    /// <summary>
    /// By default, creates a window configuration item with an opening date of 01/06, a deadline date of 01/07
    /// and a closing date of 01/08.
    /// </summary>
    private static Window CreateWindow(
        WindowType windowType,
        Date? openingDateConfig = null,
        Date? deadlineDateConfig = null,
        Date? closingDateConfig = null)
    {
        var opening = openingDateConfig ?? new Date { Day = 1, Month = 1 };
        var deadline = deadlineDateConfig ?? new Date { Day = 1, Month = 7 };
        var closing = closingDateConfig ?? new Date { Day = 1, Month = 8 };

        return new Window
        {
            WindowType = windowType,
            OpeningDate = opening,
            DeadlineDate = deadline,
            ClosingDate = closing
        };
    }
}