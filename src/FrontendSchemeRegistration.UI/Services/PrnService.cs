using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services
{
    public class PrnService : IPrnService
    {
        private readonly IWebApiGatewayClient _webApiGatewayClient;
        private Guid _dummyOrganisationId = new Guid("274FD8A3-F5CB-4DE9-A1AA-C12E7F77FA33");

        public PrnService(IWebApiGatewayClient webApiGatewayClient)
        {
            _webApiGatewayClient = webApiGatewayClient;
        }

        // Used by "View all PRNs and PERNs" page
        public async Task<PrnListViewModel> GetAllPrnsAsync()
        {
            var model = new PrnListViewModel();
            List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsByOrganisationExternalIdAsync(_dummyOrganisationId);
            List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();
            model.Prns = prnViewModels.Take(9).ToList();
            return model;
        }

        // Used by "Accept or reject PRNs and PERNs" page
        public async Task<PrnListViewModel> GetPrnsAwaitingAcceptanceAsync()
        {
            var model = new PrnListViewModel();
            List<PrnModel> serverResponse = await _webApiGatewayClient.GetPrnsByOrganisationExternalIdAsync(_dummyOrganisationId);
            List<PrnViewModel> prnViewModels = serverResponse.Select(item => (PrnViewModel)item).ToList();

            model.Prns = prnViewModels.Where(x => x.ApprovalStatus == "6").ToList();
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
    }
}