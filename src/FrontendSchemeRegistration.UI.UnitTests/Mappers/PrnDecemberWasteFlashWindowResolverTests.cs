namespace FrontendSchemeRegistration.UI.UnitTests.Mappers;

using System.Globalization;
using Application.DTOs.Prns;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using UI.Mappers;

public class PrnDecemberWasteFlashWindowResolverTests
{
    private FakeTimeProvider _fakeTimeProvider;
    private PrnDecemberWasteFlashWindowResolver _sut;

    public static IEnumerable<TestCaseData> ResolverCases =>
    [
        // Dec 2025: immediate window is Dec 2025 – Jan 2026
        new(new FlashWindowCase("2025-12-18", true, "2025-12-01", true)),
        new(new FlashWindowCase("2025-12-18", true, "2025-12-31", true)),
        new(new FlashWindowCase("2026-01-14", true, "2025-12-15", true)),
        new(new FlashWindowCase("2025-01-10", true, "2025-12-15", false)),
        new(new FlashWindowCase("2024-12-18", true, "2025-12-15", false)),
        new(new FlashWindowCase("2025-12-18", false, "2025-12-15", false)),

        // Jan 2026: immediate window is Dec 2025 – Jan 2026 (not Jan 2025)
        new(new FlashWindowCase("2025-12-18", true, "2026-01-01", true)),
        new(new FlashWindowCase("2026-01-14", true, "2026-01-31", true)),
        new(new FlashWindowCase("2025-01-10", true, "2026-01-31", false)),
        new(new FlashWindowCase("2024-12-15", true, "2026-01-31", false)),

        // Outside Dec/Jan: never flash
        new(new FlashWindowCase("2025-12-18", true, "2025-11-30", false)),
        new(new FlashWindowCase("2025-12-18", true, "2026-02-01", false)),
        new(new FlashWindowCase("2026-06-15", true, "2026-06-15", false)),

        // Dec 2026 / Jan 2027 window
        new(new FlashWindowCase("2026-12-15", true, "2026-12-15", true)),
        new(new FlashWindowCase("2027-01-12", true, "2027-01-12", true)),
        new(new FlashWindowCase("2025-12-15", true, "2026-12-15", false)),
        new(new FlashWindowCase("2026-01-14", true, "2027-01-12", false)),
    ];

    [SetUp]
    public void Setup()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _sut = new PrnDecemberWasteFlashWindowResolver(_fakeTimeProvider);
    }

    [Test]
    [TestCaseSource(nameof(ResolverCases))]
    public void Resolve_maps_correctly(FlashWindowCase testCase)
    {
        _fakeTimeProvider.SetUtcNow(testCase.CurrentTimeStamp);

        var prn = new PrnModel
        {
            IssueDate = testCase.IssueDate,
            DecemberWaste = testCase.IsDecemberWaste,
            AccreditationYear = testCase.AccreditationYear,
            ObligationYear = "2099"
        };

        var result = _sut.Resolve(prn, null!, false, null!);

        result.Should().Be(testCase.Expected);
    }

    [TestCase("2026", "2026-12-15", true)]
    [TestCase("2026", "2027-01-12", true)]
    [TestCase("2026", "2027-02-01", false)]
    [TestCase("2026", "2026-11-30", false)]
    [TestCase("2025", "2026-12-15", false)]
    [TestCase("2025", "2027-01-12", false)]
    [TestCase("2027", "2026-12-15", false)]
    public void Resolve_uses_accreditation_year_regardless_of_issue_date(string accreditationYear, string now, bool expected)
    {
        _fakeTimeProvider.SetUtcNow(DateTimeOffset.Parse(now + "T12:00:00Z", DateTimeFormatInfo.InvariantInfo));

        var prn = new PrnModel
        {
            // Issue date deliberately outside any Dec/Jan window.
            IssueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = true,
            AccreditationYear = accreditationYear,
            ObligationYear = "2099"
        };

        _sut.Resolve(prn, null!, false, null!).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not-a-year")]
    public void Resolve_returns_false_when_accreditation_year_missing_or_invalid(string? accreditationYear)
    {
        _fakeTimeProvider.SetUtcNow(DateTimeOffset.Parse("2026-12-15T12:00:00Z", DateTimeFormatInfo.InvariantInfo));

        var prn = new PrnModel
        {
            IssueDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            DecemberWaste = true,
            AccreditationYear = accreditationYear!
        };

        _sut.Resolve(prn, null!, false, null!).Should().BeFalse();
    }

    public record FlashWindowCase(string IssueDateText, bool IsDecemberWaste, string CurrentTime, bool Expected)
    {
        // Accreditation year is the year of the issue date in these cases, except January which belongs to the previous window.
        public string AccreditationYear => (IssueDate.Month == 1 ? IssueDate.Year - 1 : IssueDate.Year).ToString(CultureInfo.InvariantCulture);

        public DateTime IssueDate =>
            DateTime.Parse(IssueDateText, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        public DateTimeOffset CurrentTimeStamp =>
            DateTimeOffset.Parse(CurrentTime + "T12:00:00Z", DateTimeFormatInfo.InvariantInfo);
    }
}
