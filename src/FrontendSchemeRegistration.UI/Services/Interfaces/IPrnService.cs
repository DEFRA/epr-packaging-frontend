using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IPrnService
    {
        PrnListViewModel GetAllPrns();

        PrnViewModel GetPrnById(int id);

        PrnListViewModel GetPrnsAwaitingAcceptance();

        void UpdatePrnStatus(int id, string status);
    }
}