using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns
{
    [TestFixture]
    public class PrnMaterialObligationViewModelTests
    {
        [Theory]
        [TestCase(MaterialType.Aluminium, "aluminium")]
        [TestCase(MaterialType.Glass, "glass")]
        [TestCase(MaterialType.Paper, "paper_board_fibre")]
        [TestCase(MaterialType.Plastic, "plastic")]
        [TestCase(MaterialType.Steel, "steel")]
        [TestCase(MaterialType.Wood, "wood")]
        [TestCase(MaterialType.GlassRemelt, "glass_remelt")]
        [TestCase(MaterialType.RemainingGlass, "remaining_glass")]
        [TestCase(MaterialType.Totals, "totals")]
        public void MaterialNameResource_ShouldReturnCorrectString_ForGivenMaterialType(MaterialType material, string materialNameResource)
        {
            // Act
            var result = PrnMaterialObligationViewModel.MaterialNameResource(material);

            // Assert
            result.Should().BeEquivalentTo(materialNameResource);
        }

        [Theory]
        [TestCase(MaterialType.Aluminium, "aluminium")]
        [TestCase(MaterialType.Glass, "glass")]
        [TestCase(MaterialType.Paper, "paper_board_fibre")]
        [TestCase(MaterialType.Plastic, "plastic")]
        [TestCase(MaterialType.Steel, "steel")]
        [TestCase(MaterialType.Wood, "wood")]
        [TestCase(MaterialType.GlassRemelt, "glass")]
        [TestCase(MaterialType.RemainingGlass, "glass")]
        [TestCase(MaterialType.Totals, "totals")]
        public void MaterialCategoryResource_ShouldReturnCorrectString_ForGivenMaterialType(MaterialType material, string expected)
        {
            var result = PrnMaterialObligationViewModel.MaterialCategoryResource(material);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [TestCase(ObligationStatus.NoDataYet, "no_data_yet")]
        [TestCase(ObligationStatus.NotMet, "not_met")]
        [TestCase(ObligationStatus.Met, "met")]
        [TestCase((ObligationStatus)999, "no_data_yet")]
        public void StatusResource_ShouldReturnCorrectString_ForGivenObligationStatus(ObligationStatus status, string statusResource)
        {
            // Arrange
            var viewModel = new PrnMaterialObligationViewModel
            {
                Status = status
            };

            // Act
            var result = viewModel.StatusResource;

            // Assert
            result.Should().BeEquivalentTo(statusResource);
        }

        [Theory]
        [TestCase(ObligationStatus.NoDataYet, "grey")]
        [TestCase(ObligationStatus.NotMet, "yellow")]
        [TestCase(ObligationStatus.Met, "green")]
        [TestCase((ObligationStatus)999, "grey")]
        public void StatusDisplayCssColor_ShouldReturnCorrectColor_ForGivenObligationStatus(ObligationStatus status, string statusDisplayCssColor)
        {
            // Arrange
            var viewModel = new PrnMaterialObligationViewModel
            {
                Status = status
            };

            // Act
            var result = viewModel.StatusDisplayCssColor;

            // Assert
            result.Should().BeEquivalentTo(statusDisplayCssColor);
        }

        [Test]
        public void ImplicitConversion_ShouldConvertPrnMaterialTableModelToPrnMaterialTableViewModel_Correctly()
        {
            // Arrange
            var model = new PrnMaterialObligationModel
            {
                OrganisationId = Guid.NewGuid(),
                MaterialName = MaterialType.Glass.ToString(),
                ObligationToMeet = 100,
                TonnageAwaitingAcceptance = 200,
                TonnageAccepted = 300,
                TonnageOutstanding = 50,
                Status = ObligationStatus.NotMet.ToString()
            };

            // Act
            PrnMaterialObligationViewModel viewModel = model;

            // Assert
            viewModel.OrganisationId.Should().Be(model.OrganisationId);
            viewModel.MaterialName.Should().Be(MaterialType.Glass);
            viewModel.ObligationToMeet.Should().Be(model.ObligationToMeet);
            viewModel.TonnageAwaitingAcceptance.Should().Be(model.TonnageAwaitingAcceptance);
            viewModel.TonnageAccepted.Should().Be(model.TonnageAccepted);
            viewModel.TonnageOutstanding.Should().Be(model.TonnageOutstanding);
            viewModel.Status.Should().Be(ObligationStatus.NotMet);
        }

        [Test]
        public void ImplicitConversion_ShouldThrowException_ForInvalidEnumValues()
        {
            // Arrange
            var model = new PrnMaterialObligationModel
            {
                MaterialName = "InvalidMaterial",
                Status = "InvalidStatus"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                var viewModel = (PrnMaterialObligationViewModel)model;
            });
        }
    }
}
