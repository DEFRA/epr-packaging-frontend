using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IViewRenderService
    {
        Task<string> RenderViewToStringAsync(ActionContext actionContext, string viewName, object model);
    }
}
