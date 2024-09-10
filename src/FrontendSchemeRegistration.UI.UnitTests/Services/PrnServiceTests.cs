using AutoFixture.NUnit3;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Services
{
    [TestFixture]
    public class PrnServiceTests
    {
        private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
        private IPrnService _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
            _systemUnderTest = new PrnService(_webApiGatewayClientMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task GetAllPrnsAsync_ReturnsListOfPrnViewModels(List<PrnModel> data)
        {
            data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";
            _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
            var model = await _systemUnderTest.GetAllPrnsAsync();
            model.Should().NotBeNull();
        }

        [Theory]
        [AutoData]
        public async Task GetPrnsAwaitingAcceptanceAsync_ReturnsListOfPrnViewModels(List<PrnModel> data)
        {
            data[0].PrnStatus = "AWAITING ACCEPTANCE";
            data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";
            _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
            var model = await _systemUnderTest.GetPrnsAwaitingAcceptanceAsync();
            model.Should().NotBeNull();
        }

        [Theory]
        [AutoData]
        public async Task GetAllAcceptedPrnsAsync_ReturnsListOfAcceptedPrnViewModels(List<PrnModel> data)
        {
            data[0].PrnStatus = data[1].PrnStatus = "ACCEPTED";
            data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";

            _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
            var model = await _systemUnderTest.GetAllAcceptedPrnsAsync();
            model.Prns.Should().HaveCount(2);
            model.Prns.Should().AllSatisfy(x => x.ApprovalStatus.Should().Be("ACCEPTED"));
        }

        [Theory]
        [AutoData]
        public async Task AcceptPrnsAsync_CallsWebApiClientWIthCorrectParams(Guid[] ids)
        {
            _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToAcceptedAsync(ids)).Returns(Task.CompletedTask);
            await _systemUnderTest.AcceptPrnsAsync(ids);
            _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToAcceptedAsync(ids), Times.Once);
        }

        [Theory]
        [AutoData]
        public async Task AcceptPrnAsync_CallsWebApiClientWIthCorrectParams(Guid id)
        {
            _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToAcceptedAsync(id)).Returns(Task.CompletedTask);
            await _systemUnderTest.AcceptPrnAsync(id);
            _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToAcceptedAsync(id), Times.Once);
        }

        [Theory]
        [AutoData]
        public async Task GetPrnByExternalIdAsync_ReturnsPrn(PrnModel model)
        {
            model.AccreditationYear = "2024";
            _webApiGatewayClientMock.Setup(x => x.GetPrnByExternalIdAsync(model.ExternalId)).ReturnsAsync(model);
            var result = await _systemUnderTest.GetPrnByExternalIdAsync(model.ExternalId);
            _webApiGatewayClientMock.Verify(x => x.GetPrnByExternalIdAsync(model.ExternalId), Times.Once);
            result.Should().BeEquivalentTo((PrnViewModel)model);
        }

        [Theory]
        [AutoData]
        public async Task RejectPrnAsync_CallsWebApiClientWIthCorrectParams(Guid id)
        {
            _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToRejectedAsync(id)).Returns(Task.CompletedTask);
            await _systemUnderTest.RejectPrnAsync(id);
            _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToRejectedAsync(id), Times.Once);
        }
    }
}
