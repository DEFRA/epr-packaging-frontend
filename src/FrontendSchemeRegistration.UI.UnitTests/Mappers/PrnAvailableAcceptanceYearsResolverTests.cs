namespace FrontendSchemeRegistration.UI.UnitTests.Mappers;

using System.Globalization;
using Application.DTOs.Prns;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using UI.Mappers;

public class PrnAvailableAcceptanceYearsResolverTests
{
    private FakeTimeProvider _fakeTimeProvider;
    private PrnAvailableAcceptanceYearsResolver _sut;

    public static IEnumerable<TestCaseData> ResolverCases =>
    [
        // 2025 December Waste - These should all only be singular expected years as we don't support multiple acceptance years in the UI (yet)
        new(new DecemberWastePrnCase(2025, "2025-12-01", [2025])),
        new(new DecemberWastePrnCase(2025, "2026-01-31 23:59:59", [2025])),
        new(new DecemberWastePrnCase(2025, "2026-02-01", [2026])),
        new(new DecemberWastePrnCase(2025, "2026-12-31 23:59:59", [2026])),
        new(new DecemberWastePrnCase(2025, "2027-01-31 23:59:59", [2026])),
        new(new DecemberWastePrnCase(2025, "2027-02-01", [])), // Expired
        new(new DecemberWastePrnCase(2025, "2025-02-01", [2025])), // Future Dec Waste nonsense
        new(new DecemberWastePrnCase(2025, "2025-01-01", [])), // Future Dec Waste nonsense

        // 2026 December Waste - This is where we should start supporting multiple acceptance years in the UI
        new(new DecemberWastePrnCase(2026, "2026-12-01", [2026, 2027])),
        new(new DecemberWastePrnCase(2026, "2027-01-31 23:59:59", [2026, 2027])),
        new(new DecemberWastePrnCase(2026, "2027-02-01", [2027])),
        new(new DecemberWastePrnCase(2026, "2027-12-31 23:59:59", [2027])),
        new(new DecemberWastePrnCase(2026, "2028-01-31 23:59:59", [2027])),
        new(new DecemberWastePrnCase(2026, "2028-02-01", [])), // Expired
        new(new DecemberWastePrnCase(2026, "2026-02-01", [2026])), // Not yet Dec/Jan - no choice of year
        new(new DecemberWastePrnCase(2026, "2026-10-08", [2026])), // Not yet Dec/Jan - no choice of year (MO-632)
        new(new DecemberWastePrnCase(2026, "2026-11-30 23:59:59", [2026])), // Not yet Dec/Jan - no choice of year
        new(new DecemberWastePrnCase(2026, "2026-01-01", [])), // Future Dec Waste nonsense

        // 2025 Non-December Waste
        new(new BasicPrnCase(2025, "2025-12-31 23:59:59", [2025])),
        new(new BasicPrnCase(2025, "2026-01-01", [2025])),
        new(new BasicPrnCase(2025, "2026-01-31 23:59:59", [2025])),
        new(new BasicPrnCase(2025, "2026-02-01", [])), // Expired

        // 2026 Non-December Waste
        new(new BasicPrnCase(2026, "2026-12-31 23:59:59", [2026])),
        new(new BasicPrnCase(2026, "2027-01-01", [2026])),
        new(new BasicPrnCase(2026, "2027-01-31 23:59:59", [2026])),
        new(new BasicPrnCase(2026, "2027-02-01", [])), // Expired

        // Missing year
        new(new BasicPrnCase(null, "2025-12-01", [])),
        new(new DecemberWastePrnCase(null, "2025-12-01", [])),
    ];

    [TestCase("2026", "2099", "2026-12-15", new[] { 2026, 2027 })] // Accreditation year wins over a differing Obligation Year
    [TestCase("2026", "2027", "2027-01-15", new[] { 2026, 2027 })]
    [TestCase("2026", "2025", "2027-03-01", new[] { 2027 })]
    [TestCase("2026", null, "2026-12-15", new[] { 2026, 2027 })]
    [TestCase(null, "2026", "2026-12-15", new[] { 2026, 2027 })] // Falls back to Obligation Year
    [TestCase("not-a-year", "2026", "2026-12-15", new[] { 2026, 2027 })]
    public void DecemberWaste_uses_accreditation_year(string? accreditationYear, string? obligationYear, string now, int[] expected)
    {
        _fakeTimeProvider.SetUtcNow(DateTimeOffset.Parse(now + "T12:00:00Z", DateTimeFormatInfo.InvariantInfo));

        var prn = new PrnModel { AccreditationYear = accreditationYear!, ObligationYear = obligationYear!, DecemberWaste = true };

        _sut.Resolve(prn, null!, null!, null!).Should().BeEquivalentTo(expected);
    }

    [Test]
    public void NonDecemberWaste_ignores_accreditation_year()
    {
        _fakeTimeProvider.SetUtcNow(DateTimeOffset.Parse("2026-06-15T12:00:00Z", DateTimeFormatInfo.InvariantInfo));

        var prn = new PrnModel { AccreditationYear = "2099", ObligationYear = "2026", DecemberWaste = false };

        _sut.Resolve(prn, null!, null!, null!).Should().BeEquivalentTo(new[] { 2026 });
    }

    [SetUp]
    public void Setup()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _sut = new PrnAvailableAcceptanceYearsResolver(_fakeTimeProvider);
    }

    [Test]
    [TestCaseSource(nameof(ResolverCases))]
    public void AvailableYears_maps_correctly(IResolverTestCase testCase)
    {
        _fakeTimeProvider.SetUtcNow(testCase.CurrentTimeStamp);

        var prn = new PrnModel
        {
            ObligationYear = testCase.PrnYear.ToString(),
            AccreditationYear = testCase.PrnYear.ToString(),
            DecemberWaste = testCase.IsDecemberWaste
        };

        // Act
        var availableYears = _sut.Resolve(prn, null!, null!, null!);

        // Assert
        availableYears.Should().BeEquivalentTo(testCase.ExpectedYears);
    }

    [Test]
    public void DecemberWaste2026_OnFirstOfDecember_CanBeAcceptedTowards2026Or2027()
    {
        _fakeTimeProvider.SetUtcNow(DateTimeOffset.Parse("2026-12-01T00:00:00Z", CultureInfo.InvariantCulture));
        var prn = new PrnModel { ObligationYear = "2026", DecemberWaste = true };

        // Act
        var availableYears = _sut.Resolve(prn, null!, null!, null!);

        // Assert
        availableYears.Should().Contain(2027);
        availableYears.Should().BeEquivalentTo([2026, 2027]);
    }

    [Test]
    public void DecemberWaste2026_OnLastDayOfNovember_CannotBeAcceptedTowards2027()
    {
        _fakeTimeProvider.SetUtcNow(DateTimeOffset.Parse("2026-11-30T23:59:59Z", CultureInfo.InvariantCulture));
        var prn = new PrnModel { ObligationYear = "2026", DecemberWaste = true };

        // Act
        var availableYears = _sut.Resolve(prn, null!, null!, null!);

        // Assert
        availableYears.Should().BeEquivalentTo([2026]);
    }

    public interface IResolverTestCase
    {
        int? PrnYear { get; }
        bool IsDecemberWaste { get; }  
        DateTime CurrentTimeStamp { get; }
        int[] ExpectedYears { get; } 
    }

    public record BasicPrnCase(int? PrnYear, string CurrentTime, int[] ExpectedYears) : IResolverTestCase
    {
        public bool IsDecemberWaste => false;

        public DateTime CurrentTimeStamp => DateTime.Parse(CurrentTime, DateTimeFormatInfo.InvariantInfo);
    }

    public record DecemberWastePrnCase(int? PrnYear,  string CurrentTime, int[] ExpectedYears) : IResolverTestCase
    {
        public bool IsDecemberWaste => true;

        public DateTime CurrentTimeStamp => DateTime.Parse(CurrentTime, DateTimeFormatInfo.InvariantInfo);
    }
}