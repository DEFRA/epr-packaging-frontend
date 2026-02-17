namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using Application.DTOs.Submission;
using Application.Enums;
using Microsoft.Extensions.Time.Testing;
using UI.Services;

[TestFixture]
public class ReferenceNumberBuilderTests
{
    [Test]
    [TestCase(2025, 12, 31, "2025", "January", "December", "PEPROrg25P1L", "large")]
    [TestCase(2025, 12, 31, "2026", "January", "December", "PEPROrg26P1S", "small")]
    [TestCase(2025, 12, 31, "2027", "January", "December", "PEPROrg27P1S", "small")]
    [TestCase(2025, 12, 31, "2025", "January", "November", "PEPROrg25P2S", "small")]
    [TestCase(2026, 12, 31, "2026", "January", "January",  "PEPROrg26P2S", "small")]
    [TestCase(2026, 02, 28, "2026", "January", "February", "PEPROrg26P1S", "small")]
    [TestCase(2028, 02, 29, "2028", "January", "February", "PEPROrg28P1S", "small")]
    [TestCase(2028, 02, 29, "2028", "January", "Feb",      "PEPROrg28P1S", "small")]
    [TestCase(2028, 03, 01, "2028", "January", "Feb",      "PEPROrg28P2S", "bobby mcbobface")]
    [TestCase(2025, 10, 31, "2026", "January", "July",     "PEPROrg26P1S", "small")]
    public void BuildReferenceNumber_DirectProducer_ReturnsReferenceNumber(int nowYear, int nowMonth, int nowDay, string subYear, string startMonth, string endMonth, string expected, string journey)
    {
        var tp = new FakeTimeProvider();
        tp.SetUtcNow(new DateTimeOffset(nowYear, nowMonth, nowDay, 0, 0, 1, TimeSpan.Zero));
        var sp = new SubmissionPeriod
        {
            Year = subYear,
            EndMonth = endMonth,
            StartMonth = startMonth
        };
        
        var refNo = ReferenceNumberBuilder.Build(sp, "Org", tp, false, 0, journey);
        
        Assert.That(refNo, Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase(2025, 12, 31, "2025", "January", "December", "PEPROrg44425P1L")]
    [TestCase(2025, 12, 31, "2026", "January", "December", "PEPROrg44426P1L")]
    [TestCase(2025, 12, 31, "2027", "January", "December", "PEPROrg44427P1L")]
    [TestCase(2025, 12, 31, "2025", "January", "November", "PEPROrg44425P2L")]
    [TestCase(2026, 12, 31, "2026", "January", "January",  "PEPROrg44426P2L")]
    [TestCase(2026, 02, 28, "2026", "January", "February", "PEPROrg44426P1L")]
    [TestCase(2028, 02, 29, "2028", "January", "February", "PEPROrg44428P1L")]
    [TestCase(2028, 02, 29, "2028", "January", "Feb",      "PEPROrg44428P1L")]
    [TestCase(2028, 03, 01, "2028", "January", "Feb",      "PEPROrg44428P2L")]
    [TestCase(2025, 10, 31, "2026", "January", "July",     "PEPROrg44426P1L")]
    public void BuildReferenceNumber_ForCSO_ReturnsReferenceNumber(int nowYear, int nowMonth, int nowDay, string subYear, string startMonth, string endMonth, string expected)
    {
        var isCso = true;
        var rowNumber = 444;
        
        var tp = new FakeTimeProvider();
        tp.SetUtcNow(new DateTimeOffset(nowYear, nowMonth, nowDay, 0, 0, 1, TimeSpan.Zero));
        var sp = new SubmissionPeriod
        {
            Year = subYear,
            EndMonth = endMonth,
            StartMonth = startMonth
        };
        
        var refNo = ReferenceNumberBuilder.Build(sp, "Org", tp, isCso , rowNumber, RegistrationJourney.CsoLargeProducer.ToString());
        
        Assert.That(refNo, Is.EqualTo(expected));
    }
    
    [Test]
    [TestCase(2025, 12, 31, "2025", "January", "December", "PEPROrg25P1", null)]
    [TestCase(2025, 12, 31, "2025", "January", "December", "PEPROrg25P1", "")]
    [TestCase(2025, 12, 31, "2025", "January", "December", "PEPROrg25P1", "   ")]
    public void BuildReferenceNumber_WhenRegistrationJourneyIsNullOrEmpty_DoesNotAppendLOrS(int nowYear, int nowMonth, int nowDay, string subYear, string startMonth, string endMonth, string expected, string? journey)
    {
        var tp = new FakeTimeProvider();
        tp.SetUtcNow(new DateTimeOffset(nowYear, nowMonth, nowDay, 0, 0, 1, TimeSpan.Zero));
        var sp = new SubmissionPeriod
        {
            Year = subYear,
            EndMonth = endMonth,
            StartMonth = startMonth
        };
        
        var refNo = ReferenceNumberBuilder.Build(sp, "Org", tp, false, 0, journey);
        
        Assert.That(refNo, Is.EqualTo(expected));
    }
}