using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Services
{
    public class DownloadPrnService : IDownloadPrnService
    {
        private readonly IPrnService _prnService;
        private readonly IViewRenderService _viewRenderService;

        public DownloadPrnService(IPrnService prnService, IViewRenderService viewRenderService)
        {
            _prnService = prnService;
            _viewRenderService = viewRenderService;
        }

        public async Task<IActionResult> DownloadPrnAsync(Guid id, string viewName, ActionContext actionContext)
        {
            var prn = await _prnService.GetPrnForPdfByExternalIdAsync(id);

            string htmlContent = await _viewRenderService.RenderViewToStringAsync(actionContext, viewName, prn);

            return new OkObjectResult(new { fileName = prn.PrnOrPernNumber, htmlContent });
        }
    }
}
