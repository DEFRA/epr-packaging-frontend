using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IPrnService
    {
        public AcceptedPernsOrPrnsViewModel? GetPrn(string prnOrPernNumber);
    }
}
