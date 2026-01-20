namespace FrontendSchemeRegistration.UI.UnitTests.Mappers;

using Application.DTOs.Prns;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using UI.Mappers;

public class PrnAvailableAcceptanceYearsResolverTests
{
    private FakeTimeProvider _fakeTimeProvider;
    private PrnAvailableAcceptanceYearsResolver _sut;

    [SetUp]
    public void Setup()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _sut = new PrnAvailableAcceptanceYearsResolver(_fakeTimeProvider);
    }

    [Test]
    [TestCaseSource(nameof(ResolverCases))]
    public void AvailableYears_maps_correctly(ResolverTestCase testCase)
    {
        _fakeTimeProvider.SetUtcNow(testCase.CurrentTime);

        var prn = new PrnModel
        {
            ObligationYear = testCase.ObligationYear.ToString(),
            DecemberWaste = testCase.IsDecemberWaste
        };

        // Act
        var availableYears = _sut.Resolve(prn, null!, null!, null!);

        // Assert
        availableYears.Should().BeEquivalentTo(testCase.ExpectedYears);
    }

    public static IEnumerable<TestCaseData> ResolverCases =>
    [
        // 2025 December Waste
        new (new ResolverTestCase (2025, true, DateTime.Parse("2026-01-01"), [2025])),
        new (new ResolverTestCase (2025, true, DateTime.Parse("2026-01-31 23:59:59"), [2025])),
        new (new ResolverTestCase (2025, true, DateTime.Parse("2026-02-01"), [2026])),
        new (new ResolverTestCase (2025, true, DateTime.Parse("2026-12-31 23:59:59"), [2026])),

        // After 2025 December Waste
        new (new ResolverTestCase (2026, true, DateTime.Parse("2026-01-01"), [2026, 2027])),
        new (new ResolverTestCase (2026, true, DateTime.Parse("2026-12-31 23:59:59"), [2026, 2027])),
        new (new ResolverTestCase (2026, true, DateTime.Parse("2027-01-31 23:59:59"), [2026, 2027])),
        new (new ResolverTestCase (2026, true, DateTime.Parse("2027-02-01"), [2027])),

        // 2025 Non-December Waste
        new (new ResolverTestCase (2025, false, DateTime.Parse("2026-01-01"), [2025])),
        new (new ResolverTestCase (2025, false, DateTime.Parse("2026-01-31 23:59:59"), [2025])),
        new (new ResolverTestCase (2025, false, DateTime.Parse("2026-02-01"), [])),
        new (new ResolverTestCase (2026, false, DateTime.Parse("2026-01-01"), [])),
        new (new ResolverTestCase (2026, false, DateTime.Parse("2026-02-01"), [2026])),

        // After 2025 Non-December Waste
        new (new ResolverTestCase (2026, false, DateTime.Parse("2026-01-01"), [])),
        new (new ResolverTestCase (2026, false, DateTime.Parse("2026-12-31 23:59:59"), [2026])),
        new (new ResolverTestCase (2026, false, DateTime.Parse("2027-01-31 23:59:59"), [])),
        new (new ResolverTestCase (2026, false, DateTime.Parse("2027-02-01"), [])),
        new (new ResolverTestCase (2027, false, DateTime.Parse("2027-01-31 23:59:59"), [2027])),
        new (new ResolverTestCase (2027, false, DateTime.Parse("2027-02-01"), [2027])),
    ];

    public record ResolverTestCase(int ObligationYear, bool IsDecemberWaste, DateTime CurrentTime, int[] ExpectedYears);
}