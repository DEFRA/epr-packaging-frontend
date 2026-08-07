namespace FrontendSchemeRegistration.Application.UnitTests.Extensions;

using Application.Extensions;
using FluentAssertions;

[TestFixture]
public class ObligationYearOptionsTests
{
    [TestCase(2025, new[] { 2026, 2025 })]
    [TestCase(2026, new[] { 2027, 2026, 2025 })]
    [TestCase(2027, new[] { 2028, 2027, 2026, 2025 })]
    [TestCase(2023, new[] { 2024, 2023 })]
    public void GetSelectableYears_Returns_Future_Current_And_Historical_Newest_First(int currentYear, int[] expected)
    {
        var result = ObligationYearOptions.GetSelectableYears(currentYear);

        result.Should().Equal(expected);
    }
}
