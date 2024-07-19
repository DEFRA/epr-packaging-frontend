using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IPrnService
    {
        PrnAcceptViewModel GetAcceptPrnById(int id);

        PrnListViewModel GetAllPrns();

        PrnViewModel GetPrnById(int id);

        PrnListViewModel GetPrnsAwaitingAcceptance();
     }
}