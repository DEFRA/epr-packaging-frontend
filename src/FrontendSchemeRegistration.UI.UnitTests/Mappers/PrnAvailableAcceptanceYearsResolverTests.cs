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
        new(new DecemberWastePrnCase("2025-12-01", "2026-01-01", [2025])),
        new(new DecemberWastePrnCase("2025-12-01", "2026-01-31 23:59:59", [2025])),
        
        new(new DecemberWastePrnCase("2025-12-01", "2026-02-01", [2026])),
        new(new DecemberWastePrnCase("2025-12-01", "2026-12-31 23:59:59", [2026])),
        new(new DecemberWastePrnCase("2025-12-01", "2027-01-31 23:59:59", [2026])),
        
        new(new DecemberWastePrnCase("2025-12-01", "2027-02-01", [])), // Expired
        new(new DecemberWastePrnCase("2025-01-01", "2025-12-01", [])), // Issued before December (nonsense)
        new(new DecemberWastePrnCase("2025-11-30 23:59:59", "2025-12-01", [])), // Issued before December (nonsense)
        new(new DecemberWastePrnCase("2026-12-01", "2025-12-31 23:59:59", [])), // Issued for next December (nonsense)
        
        // 2026 December Waste - This is where we should start supporting multiple acceptance years in the UI
        new(new DecemberWastePrnCase("2026-12-01", "2027-01-01", [2026, 2027])),
        new(new DecemberWastePrnCase("2026-12-01", "2027-01-31 23:59:59", [2026, 2027])),
        
        new(new DecemberWastePrnCase("2026-12-01", "2027-02-01", [2027])),
        new(new DecemberWastePrnCase("2026-12-01", "2027-12-31 23:59:59", [2027])),
        new(new DecemberWastePrnCase("2026-12-01", "2028-01-31 23:59:59", [2027])),
        
        new(new DecemberWastePrnCase("2026-12-01", "2028-02-01", [])), // Expired
        new(new DecemberWastePrnCase("2026-01-01", "2026-12-01", [])), // Issued before December (nonsense)
        new(new DecemberWastePrnCase("2026-11-30 23:59:59", "2026-12-01", [])), // Issued before December (nonsense)
        new(new DecemberWastePrnCase("2027-12-01", "2026-12-31 23:59:59", [])), // Issued for next December (nonsense)

        // 2025 Non-December Waste
        new(new BasicPrnCase("2025-01-01", "2025-12-31 23:59:59", [2025])),
        new(new BasicPrnCase("2025-01-01", "2026-01-01", [2025])),
        new(new BasicPrnCase("2025-01-01", "2026-01-31 23:59:59", [2025])),
        new(new BasicPrnCase("2025-01-01", "2026-02-01", [])), // Expired

        new(new BasicPrnCase("2025-02-01", "2025-12-31 23:59:59", [2025])),
        new(new BasicPrnCase("2025-02-01", "2026-01-01", [2025])),
        new(new BasicPrnCase("2025-02-01", "2026-01-31 23:59:59", [2025])),
        new(new BasicPrnCase("2025-02-01", "2026-02-01", [])), // Expired

        // 2025 Non-December Waste Issued during December
        new(new BasicPrnCase("2025-12-01", "2025-12-31 23:59:59", [2025])),
        new(new BasicPrnCase("2025-12-01", "2026-01-01", [2025])),
        new(new BasicPrnCase("2025-12-01", "2026-01-31 23:59:59", [2025])),
        new(new BasicPrnCase("2025-12-01", "2026-02-01", [])), // Expired

        // 2026 Non-December Waste
        new(new BasicPrnCase("2026-01-01", "2026-12-31 23:59:59", [2026])),
        new(new BasicPrnCase("2026-01-01", "2027-01-01", [2026])),
        new(new BasicPrnCase("2026-01-01", "2027-01-31 23:59:59", [2026])),
        new(new BasicPrnCase("2026-01-01", "2027-02-01", [])), // Expired

        new(new BasicPrnCase("2026-02-01", "2026-12-31 23:59:59", [2026])),
        new(new BasicPrnCase("2026-02-01", "2027-01-01", [2026])),
        new(new BasicPrnCase("2026-02-01", "2027-01-31 23:59:59", [2026])),
        new(new BasicPrnCase("2026-02-01", "2027-02-01", [])), // Expired
    ];

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
            IssueDate = testCase.IssueDateTimeStamp,
            DecemberWaste = testCase.IsDecemberWaste
        };

        // Act
        var availableYears = _sut.Resolve(prn, null!, null!, null!);

        // Assert
        availableYears.Should().BeEquivalentTo(testCase.ExpectedYears);
    }

    public interface IResolverTestCase
    {
        DateTime IssueDateTimeStamp { get; }
        bool IsDecemberWaste { get; }  
        DateTime CurrentTimeStamp { get; }
        int[] ExpectedYears { get; } 
    }

    public record BasicPrnCase(string IssueDate, string CurrentTime, int[] ExpectedYears) : IResolverTestCase
    {
        public DateTime IssueDateTimeStamp => DateTime.Parse(IssueDate, DateTimeFormatInfo.InvariantInfo);

        public bool IsDecemberWaste => false;

        public DateTime CurrentTimeStamp => DateTime.Parse(CurrentTime, DateTimeFormatInfo.InvariantInfo);
    }

    public record DecemberWastePrnCase(string IssueDate, string CurrentTime, int[] ExpectedYears) : IResolverTestCase
    {
        public DateTime IssueDateTimeStamp => DateTime.Parse(IssueDate, DateTimeFormatInfo.InvariantInfo);

        public bool IsDecemberWaste => true;

        public DateTime CurrentTimeStamp => DateTime.Parse(CurrentTime, DateTimeFormatInfo.InvariantInfo);
    }
}