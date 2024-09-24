using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.Prns
{
    public class PrnSearchResultViewModelTests
    {
        [Test]
        public void Implicit_Operator_Maps_Fields()
        {
            PrnModel prn = new PrnModel
            {
                ExternalId = Guid.NewGuid(),
                PrnNumber = "ER1234599M",
                MaterialName = "Wood",
                IssueDate = DateTime.Now,
                DecemberWaste = true,
                IssuedByOrg = "Valpack (EA)",
                TonnageValue = 321,
                PrnStatus = "AWAITING ACCEPTANCE"
            };

            PrnSearchResultViewModel viewModel = prn;

            viewModel.ExternalId.Should().Be(prn.ExternalId);
            viewModel.PrnOrPernNumber.Should().Be(prn.PrnNumber);
            viewModel.PrnOrPernNumber.Should().Be("ER1234599M");
            viewModel.Material.Should().Be(prn.MaterialName);
            viewModel.Material.Should().Be("Wood");
            viewModel.DateIssued.Should().Be(prn.IssueDate);
            viewModel.IsDecemberWaste.Should().Be(prn.DecemberWaste);
            viewModel.IsDecemberWaste.Should().Be(true);
            viewModel.IssuedBy.Should().Be(prn.IssuedByOrg);
            viewModel.IssuedBy.Should().Be("Valpack (EA)");
            viewModel.Tonnage.Should().Be(prn.TonnageValue);
            viewModel.Tonnage.Should().Be(321);
            viewModel.ApprovalStatus.Should().Be(prn.PrnStatus);
            viewModel.ApprovalStatus.Should().Be("AWAITING ACCEPTANCE");

        }
    }
}
