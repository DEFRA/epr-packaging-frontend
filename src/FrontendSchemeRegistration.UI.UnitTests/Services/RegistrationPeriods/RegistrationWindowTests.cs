using AutoFixture;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Services.RegistrationPeriods;
using Microsoft.Extensions.Time.Testing;

namespace FrontendSchemeRegistration.UI.UnitTests.Services.RegistrationPeriods;

using FluentAssertions;

[TestFixture]
public class RegistrationWindowTests
{
    [TestCase("2026-05-01", RegistrationWindowStatus.PriorToOpening)]
    [TestCase("2026-06-01", RegistrationWindowStatus.OpenAndNotLate)]
    [TestCase("2026-06-30", RegistrationWindowStatus.OpenAndNotLate)]
    [TestCase("2026-07-01", RegistrationWindowStatus.OpenAndLate)]
    [TestCase("2026-07-31", RegistrationWindowStatus.OpenAndLate)]
    [TestCase("2026-08-01", RegistrationWindowStatus.Closed)]
    [TestCase("2026-08-02", RegistrationWindowStatus.Closed)]
    public async Task WHEN_GetRegistrationWindowStatus_called_THEN_ReturnCorrectRegistrationWindowStatus(DateTime currentDate, RegistrationWindowStatus expectedResult)
    {
        // arrange
        var openingDate = new DateTime(2026, 6, 1);
        var deadlineDate = new DateTime(2026, 7, 1);
        var closingDate = new DateTime(2026, 8, 1);
        
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(currentDate);
        var sut = new RegistrationWindow(timeProvider, RegistrationJourney.CsoLargeProducer, 2026, openingDate, deadlineDate, closingDate);
        
        // act
        var result = sut.GetRegistrationWindowStatus();
        
        // assert
        result.Should().Be(expectedResult);
    }

    [TestCase(RegistrationJourney.CsoLargeProducer, true)]
    [TestCase(RegistrationJourney.CsoSmallProducer, true)]
    [TestCase(RegistrationJourney.DirectLargeProducer, false)]
    [TestCase(RegistrationJourney.DirectSmallProducer, false)]
    public void WHEN_RegistrationWindowConstructorWithJourney_called_THEN_IsCsoIsCorrectlySet(RegistrationJourney journey, bool expectedIsCso)
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var openingDate = new DateTime(2026, 6, 1);
        var deadlineDate = new DateTime(2026, 7, 1);
        var closingDate = new DateTime(2026, 8, 1);

        // act
        var sut = new RegistrationWindow(timeProvider, journey, 2026, openingDate, deadlineDate, closingDate);

        // assert
        sut.IsCso.Should().Be(expectedIsCso);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void WHEN_RegistrationWindowConstructorWithIsCso_called_THEN_IsCsoIsCorrectlySet(bool isCso)
    {
        // arrange
        var timeProvider = new FakeTimeProvider();
        var openingDate = new DateTime(2026, 6, 1);
        var deadlineDate = new DateTime(2026, 7, 1);
        var closingDate = new DateTime(2026, 8, 1);

        // act
        var sut = new RegistrationWindow(timeProvider, isCso, 2026, openingDate, deadlineDate, closingDate);

        // assert
        sut.IsCso.Should().Be(isCso);
        sut.Journey.Should().BeNull();
    }
}