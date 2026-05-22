using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.UI.Extensions;

namespace FrontendSchemeRegistration.UI.UnitTests.Extensions
{
    [TestFixture]
    public class ComplianceSchemeMemberExtensionTests
    {
        [Test]
        public void GetOnlineMarketPlaces_FiltersOutMembersWithZeroFee_ReturnsPositiveFees()
        {
            var members = new List<ComplianceSchemePaymentCalculationResponseMember>
            {
                new() { MemberId = "a", MemberOnlineMarketPlaceFee = 100 },
                new() { MemberId = "b", MemberOnlineMarketPlaceFee = 0 },
                new() { MemberId = "c", MemberOnlineMarketPlaceFee = 250 }
            };

            var result = members.GetOnlineMarketPlaces();

            result.Should().HaveCount(2);
            result.Should().Contain(new[] { 100, 250 });
        }

        [Test]
        public void GetClosedLoopRecyclers_FiltersOutMembersWithZeroFee_ReturnsPositiveFees()
        {
            var members = new List<ComplianceSchemePaymentCalculationResponseMember>
            {
                new() { MemberId = "a", MemberClosedLoopRecyclingFee = 200 },
                new() { MemberId = "b", MemberClosedLoopRecyclingFee = 0 },
                new() { MemberId = "c", MemberClosedLoopRecyclingFee = 50 }
            };

            var result = members.GetClosedLoopRecyclers();

            result.Should().HaveCount(2);
            result.Should().Contain(new[] { 200, 50 });
        }

        [Test]
        public void GetClosedLoopRecyclers_WhenNoMembersHaveFee_ReturnsEmpty()
        {
            var members = new List<ComplianceSchemePaymentCalculationResponseMember>
            {
                new() { MemberId = "a", MemberClosedLoopRecyclingFee = 0 },
                new() { MemberId = "b", MemberClosedLoopRecyclingFee = 0 }
            };

            var result = members.GetClosedLoopRecyclers();

            result.Should().BeEmpty();
        }
    }
}
