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
            ObligationYear = "2099"
        };

        var result = _sut.Resolve(prn, null!, false, null!);

        result.Should().Be(testCase.Expected);
    }

    public record FlashWindowCase(string IssueDateText, bool IsDecemberWaste, string CurrentTime, bool Expected)
    {
        public DateTime IssueDate => DateTime.Parse(IssueDateText, DateTimeFormatInfo.InvariantInfo);

        public DateTime CurrentTimeStamp => DateTime.Parse(CurrentTime, DateTimeFormatInfo.InvariantInfo);
    }
}
