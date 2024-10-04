using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services
{

    public class PrnService : IPrnService
    {
        private readonly IWebApiGatewayClient _webApiGatewayClient;

        public PrnService(IWebApiGatewayClient webApiGatewayClient)
        {
            _webApiGatewayClient = webApiGatewayClient;
        }

        // Used by "View all PRNs and PERNs" page
        public async Task<PrnListViewModel> GetAllPrnsAsync()
        {
            var model = new PrnListViewModel();
            List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
            List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();
            model.Prns = prnViewModels.OrderByDescending(x => x.DateIssued).Take(9).ToList();
            return model;
        }

        // Used by "Accept or reject PRNs and PERNs" page
        public async Task<PrnListViewModel> GetPrnsAwaitingAcceptanceAsync()
        {
            var model = new PrnListViewModel();
            List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
            List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();

            model.Prns = prnViewModels.Where(x => x.ApprovalStatus.EndsWith("ACCEPTANCE")).OrderBy(x => x.Material).ThenByDescending(x => x.DateIssued).ToList();
            return model;
        }

        public async Task<PrnViewModel> GetPrnByExternalIdAsync(Guid id)
        {
            var serverResponse = await _webApiGatewayClient.GetPrnByExternalIdAsync(id);
            PrnViewModel model = serverResponse;
            return model;
        }

        public async Task AcceptPrnAsync(Guid id)
        {
            await _webApiGatewayClient.SetPrnApprovalStatusToAcceptedAsync(id);
        }

        public async Task AcceptPrnsAsync(Guid[] ids)
        {
            await _webApiGatewayClient.SetPrnApprovalStatusToAcceptedAsync(ids);
        }

        public async Task<PrnListViewModel> GetAllAcceptedPrnsAsync()
        {
            var model = new PrnListViewModel();
            // this need refactoring when getorg api support filtering
            List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsForLoggedOnUserAsync();
            List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();

            model.Prns = prnViewModels.Where(x => x.ApprovalStatus.EndsWith("ACCEPTED")).ToList();
            return model;
        }

        public async Task RejectPrnAsync(Guid id)
        {
            await _webApiGatewayClient.SetPrnApprovalStatusToRejectedAsync(id);
        }

        public async Task<PrnSearchResultListViewModel> GetPrnSearchResultsAsync(SearchPrnsViewModel request)
        {
            PaginatedRequest paginatedRequest = request;
            var prnSearchResults = await _webApiGatewayClient.GetSearchPrnsAsync(paginatedRequest);

            var pagingDetail = new PagingDetail
            {
                CurrentPage = prnSearchResults.CurrentPage,
                PageSize = request.PageSize,
                TotalItems = prnSearchResults.TotalItems,
                TotalPages = prnSearchResults.PageCount
            };

            return new PrnSearchResultListViewModel
            {
                SearchString = prnSearchResults.SearchTerm,
                ActivePageOfResults = prnSearchResults.Items.Select(item => (PrnSearchResultViewModel)item).ToList(),
                PagingDetail = pagingDetail,
                TypeAhead = prnSearchResults.TypeAhead,
                SelectedFilter = request.FilterBy,
                SelectedSort = request.SortBy
            };
        }

        public async Task<AwaitingAcceptancePrnsViewModel> GetPrnAwaitingAcceptanceSearchResultsAsync(SearchPrnsViewModel request)
        {
            PaginatedRequest paginatedRequest = request;
            var prnSearchResults = await _webApiGatewayClient.GetSearchPrnsAsync(paginatedRequest);

            var pagingDetail = new PagingDetail
            {
                CurrentPage = prnSearchResults.CurrentPage,
                PageSize = request.PageSize,
                TotalItems = prnSearchResults.TotalItems,
                TotalPages = prnSearchResults.PageCount
            };

            return new AwaitingAcceptancePrnsViewModel
            {
                Prns = prnSearchResults.Items.Select(item => (AwaitingAcceptanceResultViewModel)item).ToList(),
                PagingDetail = pagingDetail
            };
        }

        public async Task<int> GetAwaitingAcceptancePrnsCount()
        {
            var count = 0;
            //default awaiting request and take item counts from it or else seperate api to pull count
            var searchPrns = new SearchPrnsViewModel()
            {
                FilterBy = PrnConstants.Filters.AwaitingAll
            };

            var awaitingSearchResponse = await GetPrnAwaitingAcceptanceSearchResultsAsync(searchPrns);

            count = awaitingSearchResponse.PagingDetail.TotalItems;
            
            return count;
        }
    }
}