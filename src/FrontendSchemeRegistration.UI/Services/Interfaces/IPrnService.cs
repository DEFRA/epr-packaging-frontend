using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IPrnService
    {
        Task<PrnListViewModel> GetAllPrnsAsync();

        Task<PrnViewModel> GetPrnByExternalIdAsync(Guid id);

        Task<PrnListViewModel> GetPrnsAwaitingAcceptanceAsync();

        Task AcceptPrnAsync(Guid id);
    }
}