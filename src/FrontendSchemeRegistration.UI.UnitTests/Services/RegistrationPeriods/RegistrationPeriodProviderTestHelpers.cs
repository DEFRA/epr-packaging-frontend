namespace FrontendSchemeRegistration.UI.UnitTests.Services.RegistrationPeriods;

using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Options.RegistrationPeriodPatterns;

/// <summary>
/// Test-local recreations of the deleted appsettings-pattern classes. They exist only to preserve the
/// original test-authoring shape after the provider was refactored to receive pre-computed
/// SubmissionPeriodDetails from the payment facade instead of parsing yearly offsets from config.
/// </summary>
internal class TestPattern
{
    public int InitialRegistrationYear { get; set; }
    public int? FinalRegistrationYear { get; set; }
    public List<TestWindow> Windows { get; set; } = new();
}

internal class TestWindow
{
    public WindowType WindowType { get; set; }
    public TestDate OpeningDate { get; set; } = new();
    public TestDate DeadlineDate { get; set; } = new();
    public TestDate ClosingDate { get; set; } = new();
}

internal class TestDate
{
    public int Day { get; set; }
    public int Month { get; set; }
    public int YearOffset { get; set; }
}

internal static class TestPatternExtensions
{
    /// <summary>
    /// Mirrors the parsing that <see cref="FrontendSchemeRegistration.UI.Services.RegistrationPeriods.RegistrationPeriodProvider"/>
    /// used to perform against appsettings, so the extensive test coverage can keep asserting the
    /// same year-offset semantics. Each resulting <see cref="SubmissionPeriodDetails"/> gets a
    /// deterministic id derived from its window slot.
    /// </summary>
    public static SubmissionPeriodDetails[] ToSubmissionPeriods(this IEnumerable<TestPattern> patterns, TimeProvider timeProvider)
    {
        var currentYear = timeProvider.GetUtcNow().Year;
        var results = new List<SubmissionPeriodDetails>();
        var id = 1;

        foreach (var pattern in patterns)
        {
            var registrationYear = pattern.InitialRegistrationYear;
            var finalYear = pattern.FinalRegistrationYear
                ?? currentYear - pattern.Windows.Min(w => w.OpeningDate.YearOffset);

            do
            {
                foreach (var window in pattern.Windows)
                {
                    results.Add(new SubmissionPeriodDetails
                    {
                        Id = id++,
                        WindowType = window.WindowType.ToString(),
                        RegistrationYear = registrationYear,
                        OpeningDate = new DateTime(registrationYear + window.OpeningDate.YearOffset, window.OpeningDate.Month, window.OpeningDate.Day, 0, 0, 0, DateTimeKind.Utc),
                        DeadlineDate = new DateTime(registrationYear + window.DeadlineDate.YearOffset, window.DeadlineDate.Month, window.DeadlineDate.Day, 0, 0, 0, DateTimeKind.Utc),
                        ClosingDate = new DateTime(registrationYear + window.ClosingDate.YearOffset, window.ClosingDate.Month, window.ClosingDate.Day, 0, 0, 0, DateTimeKind.Utc),
                    });
                }

                registrationYear++;
            } while (registrationYear <= finalYear);
        }

        return results.ToArray();
    }
}
