namespace FrontendSchemeRegistration.UI.UnitTests.Mappers;

using Application.DTOs.Prns;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using UI.Mappers;

public class IsPrnStatusEditableResolverTests
{
    private FakeTimeProvider _fakeTimeProvider;
    private IsPrnStatusEditableResolver _sut;

    [SetUp]
    public void Setup()
    {
        _fakeTimeProvider = new FakeTimeProvider();

        _sut = new IsPrnStatusEditableResolver(_fakeTimeProvider);
    }

    [TestCase("AWAITING ACCEPTANCE", 2026, 2026, false, true)]
    [TestCase("AWAITINGACCEPTANCE", 2026, 2026, false, true)]
    [TestCase("AWAITING ACCEPTANCE", 2025, 2026, true, true)]
    [TestCase("ACCEPTED", 2026, 2026, false, false)]
    [TestCase("AWAITING ACCEPTANCE", 2025, 2026, false, false)]
    [TestCase("AWAITING ACCEPTANCE", 2024, 2026, true, false)]
    public void IsStatusEditable_maps_correctly(string status, int obligationYear, int complianceYear,
        bool isDecemberWaste, bool expectedEditable)
    {
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var prn = new PrnModel
        {
            PrnStatus = status,
            ObligationYear = obligationYear.ToString(),
            DecemberWaste = isDecemberWaste
        };

        // Act
        var isStatusEditable = _sut.Resolve(prn, null!, false, null!);

        // Assert
        isStatusEditable.Should().Be(expectedEditable);
    }
}