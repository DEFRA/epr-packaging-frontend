using AutoFixture;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
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
        private static readonly IFixture _fixture = new Fixture();

        [SetUp]
        public void SetUp()
        {
            _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
            _systemUnderTest = new PrnService(_webApiGatewayClientMock.Object);
        }

        [Test]
        public async Task GetAllPrnsAsync_ReturnsListOfPrnViewModels()
        {
            var data = _fixture.CreateMany<PrnModel>().ToList();
            data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";
            _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
            var model = await _systemUnderTest.GetAllPrnsAsync();
            model.Should().NotBeNull();
        }

        [Test]
        public async Task GetPrnsAwaitingAcceptanceAsync_ReturnsListOfPrnViewModels()
        {
            var data = _fixture.CreateMany<PrnModel>().ToList();
            data[0].PrnStatus = "AWAITING ACCEPTANCE";
            data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";
            _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
            var model = await _systemUnderTest.GetPrnsAwaitingAcceptanceAsync();
            model.Should().NotBeNull();
        }

        [Test]
        public async Task GetAllAcceptedPrnsAsync_ReturnsListOfAcceptedPrnViewModels()
        {
            var data = _fixture.CreateMany<PrnModel>().ToList();
            data[0].PrnStatus = data[1].PrnStatus = "ACCEPTED";
            data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";

            _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
            var model = await _systemUnderTest.GetAllAcceptedPrnsAsync();
            model.Prns.Should().HaveCount(2);
            model.Prns.Should().AllSatisfy(x => x.ApprovalStatus.Should().Be("ACCEPTED"));
        }

        [Test]
        public async Task AcceptPrnsAsync_CallsWebApiClientWIthCorrectParams()
        {
            var ids = _fixture.CreateMany<Guid>().ToArray();
            _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToAcceptedAsync(ids)).Returns(Task.CompletedTask);
            await _systemUnderTest.AcceptPrnsAsync(ids);
            _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToAcceptedAsync(ids), Times.Once);
        }

        [Test]
        public async Task AcceptPrnAsync_CallsWebApiClientWIthCorrectParams()
        {
            var id = Guid.NewGuid();
            _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToAcceptedAsync(id)).Returns(Task.CompletedTask);
            await _systemUnderTest.AcceptPrnAsync(id);
            _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToAcceptedAsync(id), Times.Once);
        }

        [Test]
        public async Task GetPrnByExternalIdAsync_ReturnsPrn()
        {
            var model = _fixture.Create<PrnModel>();
            model.AccreditationYear = "2024";
            _webApiGatewayClientMock.Setup(x => x.GetPrnByExternalIdAsync(model.ExternalId)).ReturnsAsync(model);
            var result = await _systemUnderTest.GetPrnByExternalIdAsync(model.ExternalId);
            _webApiGatewayClientMock.Verify(x => x.GetPrnByExternalIdAsync(model.ExternalId), Times.Once);
            result.Should().BeEquivalentTo((PrnViewModel)model);
        }

        [Test]
        public async Task RejectPrnAsync_CallsWebApiClientWIthCorrectParams()
        {
            var id = Guid.NewGuid();
            _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToRejectedAsync(id)).Returns(Task.CompletedTask);
            await _systemUnderTest.RejectPrnAsync(id);
            _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToRejectedAsync(id), Times.Once);
        }

        [Test]
        public async Task GetPrnSearchResultsAsync_ReturnsMatchingPrns()
        {
            SearchPrnsViewModel request = new SearchPrnsViewModel { PageSize = 5, Search = "search me" };
            var pageOne = new List<PrnModel>();
            for (int i = 0; i < request.PageSize; i++)
            {
                pageOne.Add(_fixture.Create<PrnModel>());
            }
            var pageTwo = new List<PrnModel> { _fixture.Create<PrnModel>() };
            PaginatedResponse<PrnModel> paginatedResposne = new PaginatedResponse<PrnModel>();

            paginatedResposne.SearchTerm = request.Search;
            paginatedResposne.Items = pageTwo;
            paginatedResposne.CurrentPage = 2;
            paginatedResposne.TotalItems = pageTwo.Count + pageOne.Count;
            paginatedResposne.PageSize = request.PageSize;
            paginatedResposne.TypeAhead  = new List<string> { "prn number", "issued by" };

            _webApiGatewayClientMock.Setup(x => x.GetSearchPrnsAsync(It.IsAny<PaginatedRequest>())).ReturnsAsync(paginatedResposne);

            // Act
            var prnSearchResults = await _systemUnderTest.GetPrnSearchResultsAsync(request);

            // Assert
            prnSearchResults.SearchString.Should().Be(request.Search);
            prnSearchResults.ActivePageOfResults.Count.Should().Be(pageTwo.Count);
            prnSearchResults.PagingDetail.CurrentPage.Should().Be(paginatedResposne.CurrentPage);
            prnSearchResults.PagingDetail.PageSize.Should().Be(request.PageSize);
            prnSearchResults.PagingDetail.TotalItems.Should().Be(paginatedResposne.TotalItems);
            prnSearchResults.TypeAhead.Should().BeEquivalentTo(paginatedResposne.TypeAhead);

        }
    }
}
