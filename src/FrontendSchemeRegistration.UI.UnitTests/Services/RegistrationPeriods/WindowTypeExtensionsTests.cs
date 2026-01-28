using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options.RegistrationPeriodPatterns;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using FluentAssertions;

namespace FrontendSchemeRegistration.UI.UnitTests.Services.RegistrationPeriods;

[TestFixture]
public class WindowTypeExtensionsTests
{
    [TestCase(WindowType.CsoLargeProducer, RegistrationJourney.CsoLargeProducer)]
    [TestCase(WindowType.CsoSmallProducer, RegistrationJourney.CsoSmallProducer)]
    [TestCase(WindowType.DirectLargeProducer, null)]
    [TestCase(WindowType.DirectSmallProducer, null)]
    [TestCase(WindowType.Cso, null)]
    [TestCase(WindowType.Direct, null)]
    public void WHEN_ToRegistrationJourney_called_with_window_type_THEN_return_correct_registration_journey(
        WindowType windowType,
        RegistrationJourney? expectedJourney)
    {
        // act
        var result = windowType.ToRegistrationJourney();

        // assert
        result.Should().Be(expectedJourney);
    }
}
