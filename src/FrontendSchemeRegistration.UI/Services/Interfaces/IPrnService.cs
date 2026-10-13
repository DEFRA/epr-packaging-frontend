using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IPrnService
    {
        Task<PrnListViewModel> GetAllPrnsAsync();

        Task<PrnViewModel> GetPrnByExternalIdAsync(Guid id);

        Task<PrnViewModel> GetPrnForPdfByExternalIdAsync(Guid id);

        Task<PrnListViewModel> GetPrnsAwaitingAcceptanceAsync();

        Task AcceptPrnAsync(Guid id, string? obligationYear = null);

        Task AcceptPrnsAsync(Guid[] ids);

        Task<PrnListViewModel> GetAllAcceptedPrnsAsync();

        Task RejectPrnAsync(Guid id);

		Task<PrnSearchResultListViewModel> GetPrnSearchResultsAsync(SearchPrnsViewModel request);

        Task<AwaitingAcceptancePrnsViewModel> GetPrnAwaitingAcceptanceSearchResultsAsync(SearchPrnsViewModel request);

        /// <summary>
        ///     As <see cref="GetPrnAwaitingAcceptanceSearchResultsAsync(SearchPrnsViewModel)"/>, but only for PRNs that
        ///     can be accepted into <paramref name="obligationYear"/>. The backend cannot filter by acceptance year, so
        ///     every page is loaded, filtered and then paged here.
        /// </summary>
        Task<AwaitingAcceptancePrnsViewModel> GetPrnAwaitingAcceptanceSearchResultsForYearAsync(SearchPrnsViewModel request, int obligationYear);
        
        Task<Stream> GetPrnsCsvStreamAsync(int? obligationYear);

        Task<PrnObligationViewModel> GetRecyclingObligationsCalculation(int year, bool includeComplianceDeclarationStatus = false);
	}
}