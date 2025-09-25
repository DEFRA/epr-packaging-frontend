using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Services
{
    [ExcludeFromCodeCoverage]
    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(IRazorViewEngine viewEngine, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public Task<string> RenderViewToStringAsync(ActionContext actionContext, string viewName, object model)
        {
            var view = ValidateAndGetView(actionContext, viewName);
            return RenderViewContentAsync(actionContext, view, model);
        }

        private IView ValidateAndGetView(ActionContext actionContext, string viewName)
        {
            ArgumentNullException.ThrowIfNull(actionContext);

            if (string.IsNullOrWhiteSpace(viewName))
            {
                throw new ArgumentException("View name cannot be null or empty.", nameof(viewName));
            }

            var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: false);

            if (viewResult.View == null)
            {
                throw new InvalidOperationException($"View '{viewName}' not found.");
            }

            return viewResult.View;
        }

        private async Task<string> RenderViewContentAsync(ActionContext actionContext, IView view, object model)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var sw = new StringWriter();

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };
            viewDictionary["IsPdf"] = true;

            var tempData = new TempDataDictionary(httpContext, _serviceProvider.GetRequiredService<ITempDataProvider>());

            var viewContext = new ViewContext(
                actionContext,
                view,
                viewDictionary,
                tempData,
                sw,
                new HtmlHelperOptions());

            await view.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
