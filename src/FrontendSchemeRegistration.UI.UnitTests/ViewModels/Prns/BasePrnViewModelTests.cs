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

        [TestCase("Fibre", "Paper/board")]
        [TestCase("Paper/board", "Paper/board")]
        [TestCase("Glass", "Glass")]
        [TestCase(null, null)]
        public void MaterialGroup_AsExpected(string? material, string? expectedMaterialGroup)
        {
            var subject = new BasePrnViewModel { Material = material };

            subject.MaterialGroup.Should().Be(expectedMaterialGroup);
        }

        [TestCase("AWAITING ACCEPTANCE", true)]
        [TestCase("ACCEPTED", false)]
        public void IsAwaitingAcceptance_Returns_Expected(string approvalStatus, bool expected)
        {
            var sut = new BasePrnViewModel { ApprovalStatus = approvalStatus };

            sut.IsAwaitingAcceptance.Should().Be(expected);
        }

        [TestCase(true, "AWAITING ACCEPTANCE", true, new[] { 2025 }, true)]
        [TestCase(true, "AWAITING ACCEPTANCE", true, new[] { 2026, 2027 }, true)]
        [TestCase(true, "AWAITING ACCEPTANCE", true, new int[] { }, false)]
        [TestCase(true, "AWAITING ACCEPTANCE", false, new[] { 2026, 2027 }, false)]
        [TestCase(false, "AWAITING ACCEPTANCE", true, new[] { 2026, 2027 }, false)]
        [TestCase(true, "ACCEPTED", true, new[] { 2026, 2027 }, false)]
        public void ShouldShowDecemberWasteFlash_Returns_Expected(
            bool isDecemberWaste,
            string approvalStatus,
            bool isInDecemberWasteFlashWindow,
            int[] availableAcceptanceYears,
            bool expected)
        {
            var sut = new BasePrnViewModel
            {
                IsDecemberWaste = isDecemberWaste,
                ApprovalStatus = approvalStatus,
                IsInDecemberWasteFlashWindow = isInDecemberWasteFlashWindow,
                AvailableAcceptanceYears = availableAcceptanceYears
            };

            sut.ShouldShowDecemberWasteFlash.Should().Be(expected);
        }
    }
}
