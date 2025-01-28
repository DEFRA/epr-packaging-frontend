using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IDownloadPrnService
    {
        public Task<IActionResult> DownloadPrnAsync(Guid id, string viewName, ActionContext actionContext);
    }
}
