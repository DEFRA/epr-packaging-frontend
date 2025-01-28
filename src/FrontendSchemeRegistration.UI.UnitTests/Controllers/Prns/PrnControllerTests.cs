namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns
{
    using AutoFixture;
    using EPR.Common.Authorization.Sessions;
    using FluentAssertions;
    using FrontendSchemeRegistration.Application.Constants;
    using FrontendSchemeRegistration.UI.Constants;
    using FrontendSchemeRegistration.UI.Controllers.Prns;
    using FrontendSchemeRegistration.UI.Extensions;
    using FrontendSchemeRegistration.UI.Services.Interfaces;
    using FrontendSchemeRegistration.UI.Sessions;
    using FrontendSchemeRegistration.UI.ViewModels;
    using FrontendSchemeRegistration.UI.ViewModels.Prns;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;
    using Moq;
    using NUnit.Framework;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class PrnControllerTests
    {
        private Mock<IPrnService> _prnServiceMock;
        private Mock<IDownloadPrnService> _mockDownloadPrnService;
        private Mock<IUrlHelper> _urlHelperMock;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
        
        private PrnsController _controller;
        private static readonly IFixture _fixture = new Fixture();

        [SetUp]
        public void SetUp()
        {
            _prnServiceMock = new Mock<IPrnService>();
            _mockDownloadPrnService = new Mock<IDownloadPrnService>();
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "SearchPrns")))
                .Returns(PagePaths.Prns.Search);
            _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

            _controller = new PrnsController(_prnServiceMock.Object, _sessionManagerMock.Object, _mockDownloadPrnService.Object)
            {
                Url = _urlHelperMock.Object
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new Mock<ISession>().Object
                },
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
            };

            var tempData = new TempDataDictionary(Mock.Of<HttpContext>(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [Test]
        public async Task SearchPrns_OnFindingZeroPrns_ShouldReturnEmptyView()
        {
            // Arrange
            SearchPrnsViewModel request = new();
            PrnSearchResultListViewModel emptySearchResult = new();
            emptySearchResult.ActivePageOfResults = new();

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(emptySearchResult);

            // Act
            var response = await _controller.SearchPrns(request);

            var view = response.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().Be("SearchPrnsEmpty");
            _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Once);
        }

        [Test]
        public async Task SearchPrns_OnFindingZeroOfZeroPrns_ShouldReturnEmptyView()
        {
            // Arrange
            SearchPrnsViewModel request = new();
            request.FilterBy = "SomeFilter";
            PrnSearchResultListViewModel emptySearchResult = new();
            emptySearchResult.ActivePageOfResults = new();

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(emptySearchResult);
            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(request)).ReturnsAsync(emptySearchResult);

            // Act
            var response = await _controller.SearchPrns(request);

            // Assert
            var view = response.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().Be("SearchPrnsEmpty");
            _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Exactly(2));
        }

        [Test]
        public async Task SearchPrns_OnFindingZeroOfManyPrns_ShouldReturnView()
        {
            // Arrange
            SearchPrnsViewModel request = new();
            request.FilterBy = "SomeFilter";
            PrnSearchResultListViewModel emptySearchResult = new();
            emptySearchResult.ActivePageOfResults = new();
            var allSearchResult = _fixture.Create<PrnSearchResultListViewModel>();

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(allSearchResult);
            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(request)).ReturnsAsync(emptySearchResult);

            // Act
            var response = await _controller.SearchPrns(request);

            // Assert
            var view = response.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().BeNull();
            _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Exactly(2));
        }

        [Test]
        public async Task SearchPrns_ShouldReturnViewWithCorrectModel_WhenPageIsValid()
        {
            // Arrange
            var request = _fixture.Create<SearchPrnsViewModel>();
            var prnSearchResult = _fixture.Create<PrnSearchResultListViewModel>();
            var testUrl = _fixture.Create<string>();

            request.Page = 3;
            prnSearchResult.PagingDetail = new PagingDetail { TotalPages = 5, PagingLink = string.Empty };
            _prnServiceMock.Setup(service => service.GetPrnSearchResultsAsync(request)).ReturnsAsync(prnSearchResult);
            _urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns(testUrl);
            // Act
            var result = await _controller.SearchPrns(request);
            // Assert
            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            viewResult.Model.Should().Be(prnSearchResult);
            prnSearchResult.PagingDetail.PagingLink.Should().Contain(testUrl);
            _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        }

        [Test]
        public async Task SearchPrns_ShouldSaveToSession()
        {
            // Arrange
            var request = _fixture.Create<SearchPrnsViewModel>();
            var prnSearchResult = _fixture.Create<PrnSearchResultListViewModel>();
            _prnServiceMock.Setup(service => service.GetPrnSearchResultsAsync(request)).ReturnsAsync(prnSearchResult);
           
            // Act
            var result = await _controller.SearchPrns(request);

            // Assert
            _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
        }

        [Test]
        [TestCase(null, "enter_prn_or_pern_number")]
        [TestCase("", "enter_prn_or_pern_number")]
        [TestCase(" ", "enter_prn_or_pern_number")]
        public async Task SearchPrns_WhenButtonIsClicked_AndSearchIsEmpty_AddErrorsIntoModelState(string search, string expectedError)
        {
            var emptySearchResult = new PrnSearchResultListViewModel();
            emptySearchResult.ActivePageOfResults = new();

            var buttonRequest = new SearchPrnsViewModel
            {
                Source = "button",
                Search = search
            };

            // though the button search returns zero matches, ensure there are some results available
            var allSearchResult = _fixture.Create<PrnSearchResultListViewModel>();

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(allSearchResult);
            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(buttonRequest)).ReturnsAsync(emptySearchResult);

            // Act
            var result = await _controller.SearchPrns(buttonRequest);

            // Assert
            var view = result.Should().BeOfType<ViewResult>().Which;
            view.ViewData.ModelState.Count.Should().Be(1);
            view.ViewData.ModelState.GetModelStateEntry("search").Value.Errors.Select(x => x.ErrorMessage)
                .Should().Contain(expectedError);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task SearchPrns_WhenSearchIsEmpty_ReturnsView_WithQueryString(string search)
        {
	        // Arrange
	        var searchResults = new PrnSearchResultListViewModel
	        {
		        ActivePageOfResults = new List<PrnSearchResultViewModel>()
	        };
	        var buttonRequest = new SearchPrnsViewModel
	        {
		        Source = "button",
		        Search = search
	        };

            // though the button search returns zero matches, ensure there are some results available
            var allSearchResult = _fixture.Create<PrnSearchResultListViewModel>();

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(allSearchResult);
            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(buttonRequest)).ReturnsAsync(searchResults);

            // Act
            var result = await _controller.SearchPrns(buttonRequest);
	        // Assert
	        _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Exactly(2));
	        var view = result.Should().BeOfType<ViewResult>().Which;
	        view.Model.Should().BeEquivalentTo(searchResults);
            view.ViewData.ModelState["search"].Errors[0].ErrorMessage.Should().Be("enter_prn_or_pern_number");

	        var model = view.Model as PrnSearchResultListViewModel;
	        model.PagingDetail.PagingLink.Should().Be(string.Concat(PagePaths.Prns.Search, $"?search={search}&sortBy={buttonRequest.SortBy}&filterBy={buttonRequest.FilterBy}&page="));
        }

        [Test]
        public async Task SearchPrns_WhenSearchReturnsMatches_ReturnsView_WithQueryString()
        {
	        // Arrange
	        var request = _fixture.Create<SearchPrnsViewModel>();
	        request.Source = "button";
	        var searchResults = _fixture.Create<PrnSearchResultListViewModel>();
	        searchResults.ActivePageOfResults = new List<PrnSearchResultViewModel> { new PrnSearchResultViewModel() }; // Ensure there are matches
	        _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(searchResults);
	        var urlHelperMock = new Mock<IUrlHelper>();
	        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/expected-url");
	        _controller.Url = urlHelperMock.Object;
	        // Act
	        var result = await _controller.SearchPrns(request);
	        // Assert
	        _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Once);
	        var view = result.Should().BeOfType<ViewResult>().Which;
	        view.Model.Should().BeEquivalentTo(searchResults);
	        view.ViewData.ModelState.Count.Should().Be(0);
	        var model = view.Model as PrnSearchResultListViewModel;
	        var expectedQueryString = $"?search={request.Search}&sortBy={request.SortBy}&filterBy={request.FilterBy}&page=";
	        model.PagingDetail.PagingLink.Should().Be("/expected-url" + expectedQueryString);
            _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(PrnConstants.Filters.AwaitingAll)]
        public async Task SelectMultiplePrns_WhenDefaultFilterAndZeroMatches_ReturnsEmptyView(string filterBy)
        {
            var request = new SearchPrnsViewModel();
            request.FilterBy = filterBy;
            var zeroAwaitngPrns = new AwaitingAcceptancePrnsViewModel();
            zeroAwaitngPrns.Prns = new List<AwaitingAcceptanceResultViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnAwaitingAcceptanceSearchResultsAsync(request)).ReturnsAsync(zeroAwaitngPrns);

            // Act
            var result = await _controller.SelectMultiplePrns(request) as ViewResult;

            var view = result.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().Be("SelectMultiplePrnsEmpty");
        }

        [Test]
        public async Task SelectMultiplePrns_WhenZeroMatches_ReturnsView()
        {
            var request = new SearchPrnsViewModel();
            request.FilterBy = "NotTheDefaultFilter";
            var zeroAwaitngPrns = new AwaitingAcceptancePrnsViewModel();
            zeroAwaitngPrns.Prns = new List<AwaitingAcceptanceResultViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnAwaitingAcceptanceSearchResultsAsync(request)).ReturnsAsync(zeroAwaitngPrns);

            // Act
            var result = await _controller.SelectMultiplePrns(request) as ViewResult;

            var view = result.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().BeNull();
        }

        // Accept or reject single or multiple Prns. Step 1 of 5 zero selections
        [Test]
        public async Task SelectMultiplePrns_AddErrorsIntoModelStateIfAnyErrors()
        {
            _controller.TempData["NoPrnsSelected"] = "select_one_or_more_prns_or_perns_to_accept_them";
            var request = _fixture.Create<SearchPrnsViewModel>();
            var zeroAwaitngPrns = new AwaitingAcceptancePrnsViewModel();
            zeroAwaitngPrns.Prns = new List<AwaitingAcceptanceResultViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnAwaitingAcceptanceSearchResultsAsync(request)).ReturnsAsync(zeroAwaitngPrns);

            // Act
            var result = await _controller.SelectMultiplePrns(request);

            // Assert
            _prnServiceMock.Verify(x => x.GetPrnAwaitingAcceptanceSearchResultsAsync(request), Times.Once);
            var view = result.Should().BeOfType<ViewResult>().Which;

            view.ViewData.ModelState.Count.Should().Be(1);
            view.ViewData.ModelState.GetModelStateEntry("Error").Value.Errors.Select(x => x.ErrorMessage)
                .Should().Contain("select_one_or_more_prns_or_perns_to_accept_them");
        }

        [Test]
        public async Task SelectMultiplePrns_ShouldSaveToSessio()
        {
            var request = _fixture.Create<SearchPrnsViewModel>();
            var awaitngPrns = _fixture.Create<AwaitingAcceptancePrnsViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnAwaitingAcceptanceSearchResultsAsync(request)).ReturnsAsync(awaitngPrns);

            // Act
            var result = await _controller.SelectMultiplePrns(request);

            // Assert
            _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
        }

        [Test]
        public async Task SelectMultiplePrns_ShouldSetFilterByToAwaitingAll_IfEmptyOrNull()
        {
            var request = _fixture.Create<SearchPrnsViewModel>();
            request.FilterBy = null;
            var awaitngPrns = _fixture.Create<AwaitingAcceptancePrnsViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnAwaitingAcceptanceSearchResultsAsync(request)).ReturnsAsync(awaitngPrns);

            // Act
            var result = await _controller.SelectMultiplePrns(request);

            // Assert
            _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
            request.FilterBy.Should().Be(PrnConstants.Filters.AwaitingAll);
        }

        // Accept or reject single Prn. Step 2 of 5 when accepting or rejecting single PRN
        [Test]
        public async Task SelectSinglePrn_LoadTheStandardResponse()
        {
            _prnServiceMock.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(new PrnViewModel());

            // Act
            var result = await _controller.SelectSinglePrn(Guid.NewGuid()) as ViewResult;

            // Assert
            result.ViewName.Should().BeNull();
            result.ViewData.Model.Should().NotBeNull();

            _controller.ViewData.Should().ContainKey("BackLinkToDisplay");
            _sessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        }


        [Test]
        public async Task DownloadPrnsToCsv_WhenZeroPrns_ReturnsEmptyView()
        {
            string data = string.Empty;
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            _prnServiceMock.Setup(x => x.GetPrnsCsvStreamAsync()).ReturnsAsync(stream);

            var response = await _controller.DownloadPrnsToCsv() as ViewResult;

            var view = response.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().Be("CsvEmpty");
        }

        [TestCase("this,is,csv,test,string")]
        public async Task DownloadPrnsToCsv_ReturnsCsvFile(string data)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            _prnServiceMock.Setup(x => x.GetPrnsCsvStreamAsync()).ReturnsAsync(stream);

            var response = await _controller.DownloadPrnsToCsv();
            var file = response.Should().BeOfType<FileStreamResult>().Which;

            using (StreamReader reader = new(file.FileStream))
                reader.ReadToEnd().Should().Be(data);

            file.FileDownloadName.Should().Be("PRNDetail.csv");
            file.ContentType.Should().Be("text/csv");
        }

        [Test]
        public async Task DownloadPrn_CallsDownloadPrnAsync_AndReturnsOkObjectResult()
        {
            // Arrange
            var prnId = Guid.NewGuid();
            var expectedResult = new OkObjectResult(new { fileName = "PRN123", htmlContent = "<html><body>Sample Content</body></html>" });
            _mockDownloadPrnService
                .Setup(x => x.DownloadPrnAsync(prnId, "SelectSinglePrn", It.IsAny<ActionContext>()))
                .ReturnsAsync(expectedResult);
            // Act
            var result = await _controller.DownloadPrn(prnId) as OkObjectResult;
            // Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(new { fileName = "PRN123", htmlContent = "<html><body>Sample Content</body></html>" });
            _mockDownloadPrnService.Verify(x => x.DownloadPrnAsync(prnId, "SelectSinglePrn", It.IsAny<ActionContext>()), Times.Once);
        }
    }
}
