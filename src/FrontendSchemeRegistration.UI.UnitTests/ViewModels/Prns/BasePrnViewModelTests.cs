using FluentAssertions;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns
{
    public class BasePrnViewModelTests
    {

        [Test]
        public void DecemberWasteDisplay_When_Is_DecemberWaste_Returns_Yes()
        {
            var sut = new BasePrnViewModel { IsDecemberWaste = true };

            sut.DecemberWasteDisplay.Should().Be("Yes");
        }

        [Test]
        public void DecemberWasteDisplay_When_IsNot_DecemberWaste_Returns_No()
        {
            var sut = new BasePrnViewModel { IsDecemberWaste = false };

            sut.DecemberWasteDisplay.Should().Be("No");
        }

        [TestCase("AWAITING ACCEPTANCE", "grey")]
        [TestCase("ACCEPTED", "green")]
        [TestCase("CANCELLED", "yellow")]
        [TestCase("REJECTED", "red")]
        [TestCase("ANYTHING ELSE", "grey")]
        public void ApprovalStatusDisplayCssColour_Returns_Colour(string approvalStatus, string expectedColour)
        {
            var sut = new BasePrnViewModel { ApprovalStatus = approvalStatus };

            sut.ApprovalStatusDisplayCssColour.Should().Be(expectedColour);
        }

        [TestCase("AWAITINGACCEPTANCE", "AWAITING ACCEPTANCE")]
        [TestCase("CANCELED", "CANCELLED")]
        [TestCase("ANYTHING ELSE", "ANYTHING ELSE")]
        public void MapStatus_Returns_ApprovalStatus(string originalStatus, string expectedStatus)
        {
            BasePrnViewModel.MapStatus(originalStatus).Should().Be(expectedStatus);
        }

    }
}
