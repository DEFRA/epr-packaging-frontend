namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns
{
    using System.Threading.Tasks;
    using AutoFixture;
    using EPR.Common.Authorization.Sessions;
    using FluentAssertions;
    using FrontendSchemeRegistration.Application.Constants;
    using FrontendSchemeRegistration.UI.Controllers.Prns;
    using FrontendSchemeRegistration.UI.Extensions;
    using FrontendSchemeRegistration.UI.Services.Interfaces;
    using FrontendSchemeRegistration.UI.Sessions;
    using FrontendSchemeRegistration.UI.ViewModels;
    using FrontendSchemeRegistration.UI.ViewModels.Prns;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.Extensions.Time.Testing;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class PrnControllerTests
    {
        private Mock<IPrnService> _prnServiceMock;
        private Mock<IUrlHelper> _urlHelperMock;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
        private PrnsController _controller;
        private static readonly IFixture _fixture = new Fixture();

        [SetUp]
        public void SetUp()
        {
            _prnServiceMock = new Mock<IPrnService>();
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "SearchPrns")))
                .Returns(PagePaths.Prns.Search);
            _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();

            var fakeTimeProvider = new FakeTimeProvider();
            _controller = new PrnsController(_prnServiceMock.Object, fakeTimeProvider, _sessionManagerMock.Object)
            {
                Url = _urlHelperMock.Object
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new Mock<ISession>().Object
                }
            };
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
        public async Task HomePagePrn_CallsGetPrnsAwaitingAcceptanceAsync()
        {
            var model = _fixture.Create<PrnListViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

            var result = await _controller.HomePagePrn();

            var view = result.Should().BeOfType<ViewResult>().Which;
            view.ViewName.Should().Be("HomePagePrn");
            _prnServiceMock.VerifyAll();
        }

        [Test]
        [TestCase(null, "enter_the_exact_prn_or_pern_number")]
        [TestCase("", "enter_the_exact_prn_or_pern_number")]
        [TestCase(" ", "enter_the_exact_prn_or_pern_number")]
        public async Task SearchPrns_WhenButtonIsClicked_AndSearchIsEmpty_AddErrorsIntoModelState(string search, string expectedError)
        {
            var searchResults = new PrnSearchResultListViewModel();
            searchResults.ActivePageOfResults = new List<PrnSearchResultViewModel>();

            var request = new SearchPrnsViewModel
            {
                Source = "button",
                Search = search
            };

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(searchResults);

            // Act
            var result = await _controller.SearchPrns(request);

            // Assert
            var view = result.Should().BeOfType<ViewResult>().Which;
            view.ViewData.ModelState.Count.Should().Be(1);
            view.ViewData.ModelState.GetModelStateEntry("search").Value.Errors.Select(x => x.ErrorMessage)
                .Should().Contain(expectedError);
        }

        [Test]
        public async Task SearchPrns_WhenButtonIsClicked_AndSearchReturnsZeroMatches_AddErrorsIntoModelState()
        {
            var searchResults = new PrnSearchResultListViewModel();
            searchResults.ActivePageOfResults = new List<PrnSearchResultViewModel>();

            var request = new SearchPrnsViewModel
            {
                Source = "button",
                Search = "search term"
            };

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(searchResults);

            // Act
            var result = await _controller.SearchPrns(request);

            // Assert
            var view = result.Should().BeOfType<ViewResult>().Which;
            view.ViewData.ModelState.Count.Should().Be(1);
            view.ViewData.ModelState.GetModelStateEntry("search").Value.Errors.Select(x => x.ErrorMessage)
                .Should().Contain("no_prns_or_perns_found");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task SearchPrns_WhenSearchIsEmpty_ReturnsView_WithQueryString(string search)
        {
            var searchResults = new PrnSearchResultListViewModel();
            searchResults.ActivePageOfResults = new List<PrnSearchResultViewModel>();

            var request = new SearchPrnsViewModel
            {
                Source = "not the button",
                Search = search
            };

            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(searchResults);

            // Act
            var result = await _controller.SearchPrns(request);

            // Assert
            _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Once);
            var view = result.Should().BeOfType<ViewResult>().Which;
            view.Model.Should().BeEquivalentTo(searchResults);
            view.ViewData.ModelState.Count.Should().Be(0);
            var model = view.Model as PrnSearchResultListViewModel;
            model.PagingDetail.PagingLink.Should().Be(string.Concat(PagePaths.Prns.Search, "?page="));
        }

        [Test]
        public async Task SearchPrns_WhenSearchReturnsMatches_ReturnsView_WithQueryString()
        {
            var request = _fixture.Create<SearchPrnsViewModel>();
            var searchResults = _fixture.Create<PrnSearchResultListViewModel>();
            _prnServiceMock.Setup(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>())).ReturnsAsync(searchResults);

            // Act
            var result = await _controller.SearchPrns(request);

            // Assert
            _prnServiceMock.Verify(x => x.GetPrnSearchResultsAsync(It.IsAny<SearchPrnsViewModel>()), Times.Once);
            var view = result.Should().BeOfType<ViewResult>().Which;
            view.Model.Should().BeEquivalentTo(searchResults);
            view.ViewData.ModelState.Count.Should().Be(0);
            var model = view.Model as PrnSearchResultListViewModel;
            model.PagingDetail.PagingLink.Should().Be(string.Concat(PagePaths.Prns.Search, "?search=", request.Search, "&page="));
        }

        // Accept or reject single or multiple Prns. Step 1 of 5 zero selections
        [Test]
        public async Task SelectMultiplePrns_AddErrorsIntoModelStateIfAnyErrors()
        {
            var model = _fixture.Create<PrnListViewModel>();
            var error = _fixture.Create<string>();
            _prnServiceMock.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

            // Act
            var result = await _controller.SelectMultiplePrns(error);

            // Assert
            _prnServiceMock.Verify(x => x.GetPrnsAwaitingAcceptanceAsync(), Times.Once);
            var view = result.Should().BeOfType<ViewResult>().Which;
            view.Model.Should().BeEquivalentTo(model);
            view.ViewData.ModelState.Count.Should().Be(1);
            view.ViewData.ModelState.GetModelStateEntry("Error").Value.Errors.Select(x => x.ErrorMessage)
                .Should().Contain("select_one_or_more_prns_or_perns_to_accept_them");
        }

        [Test]
        public async Task SelectMultiplePrns_ShouldSaveToSessio()
        {
            var model = _fixture.Create<PrnListViewModel>();
            var error = _fixture.Create<string>();
            _prnServiceMock.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

            // Act
            var result = await _controller.SelectMultiplePrns(error);

            // Assert
            _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession?>()), Times.Once);
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
            _controller.ViewData.Should().ContainKey("DecemberWasteRulesApply");
            _sessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        }
    }
}
