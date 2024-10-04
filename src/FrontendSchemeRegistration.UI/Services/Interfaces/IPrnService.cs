using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IPrnService
    {
        Task<PrnListViewModel> GetAllPrnsAsync();

        Task<PrnViewModel> GetPrnByExternalIdAsync(Guid id);

        Task<PrnListViewModel> GetPrnsAwaitingAcceptanceAsync();

        Task AcceptPrnAsync(Guid id);

        Task AcceptPrnsAsync(Guid[] ids);

        Task<PrnListViewModel> GetAllAcceptedPrnsAsync();

        Task RejectPrnAsync(Guid id);

		Task<PrnSearchResultListViewModel> GetPrnSearchResultsAsync(SearchPrnsViewModel request);

        Task<AwaitingAcceptancePrnsViewModel> GetPrnAwaitingAcceptanceSearchResultsAsync(SearchPrnsViewModel request);
        Task<int> GetAwaitingAcceptancePrnsCount();
    }
}